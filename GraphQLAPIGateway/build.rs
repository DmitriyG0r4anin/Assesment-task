use std::env;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;

fn main() {
    if let Err(e) = compile_with_protoc() {
        eprintln!("Error: {}", e);
        std::process::exit(1);
    }
}

fn compile_with_protoc() -> Result<(), Box<dyn std::error::Error>> {
    if let Ok(protoc) = env::var("PROTOC") {
        eprintln!("✓ Using PROTOC from environment: {}", protoc);
        return compile_protos();
    }

    if let Ok(cache_dir) = get_cache_dir() {
        let protoc_exe = if cfg!(windows) {
            "protoc.exe"
        } else {
            "protoc"
        };
        let cached = cache_dir.join("bin").join(protoc_exe);
        if cached.exists() {
            env::set_var("PROTOC", &cached);
            eprintln!("✓ Using cached protoc: {}", cached.display());
            return compile_protos();
        }
    }

    match find_protoc_in_path() {
        Ok(path) => {
            eprintln!("✓ Found protoc in PATH: {}", path.display());
            return compile_protos();
        }
        Err(_) => {
            eprintln!("protoc not found in PATH, attempting to set up...");
        }
    }

    let protoc_path = setup_protoc()?;
    env::set_var("PROTOC", &protoc_path);
    eprintln!("✓ Using protoc at: {}", protoc_path.display());

    compile_protos()
}

fn find_protoc_in_path() -> Result<PathBuf, Box<dyn std::error::Error>> {
    let protoc_name = if cfg!(windows) {
        "protoc.exe"
    } else {
        "protoc"
    };

    let output = Command::new("which").arg(protoc_name).output();

    // Windows doesn't have 'which', try calling protoc directly
    if cfg!(windows) {
        if Command::new("protoc")
            .arg("--version")
            .output()?
            .status
            .success()
        {
            return Ok(PathBuf::from("protoc"));
        }
    } else if let Ok(out) = output {
        if out.status.success() {
            let path = String::from_utf8(out.stdout)?.trim().to_string();
            return Ok(PathBuf::from(path));
        }
    }

    Err("protoc not found in PATH".into())
}

fn setup_protoc() -> Result<PathBuf, Box<dyn std::error::Error>> {
    let os = env::consts::OS;
    let arch = env::consts::ARCH;

    let cache_dir = get_cache_dir()?;
    fs::create_dir_all(&cache_dir)?;

    let protoc_exe = if cfg!(windows) {
        "protoc.exe"
    } else {
        "protoc"
    };
    let protoc_path = cache_dir.join("bin").join(protoc_exe);

    if protoc_path.exists() {
        // Make executable on Unix
        #[cfg(unix)]
        {
            use std::fs::Permissions;
            use std::os::unix::fs::PermissionsExt;
            fs::set_permissions(&protoc_path, Permissions::from_mode(0o755))?;
        }
        return Ok(protoc_path);
    }

    download_and_extract_protoc(&cache_dir, os, arch)?;

    // Make executable on Unix
    #[cfg(unix)]
    {
        use std::fs::Permissions;
        use std::os::unix::fs::PermissionsExt;
        fs::set_permissions(&protoc_path, Permissions::from_mode(0o755))?;
    }

    if !protoc_path.exists() {
        return Err(format!(
            "protoc not found at expected location: {}",
            protoc_path.display()
        )
        .into());
    }

    Ok(protoc_path)
}

fn get_cache_dir() -> Result<PathBuf, Box<dyn std::error::Error>> {
    let cache_path = if cfg!(windows) {
        // Windows: Use AppData\Local\Temp or similar
        PathBuf::from(
            env::var("LOCALAPPDATA")
                .unwrap_or_else(|_| env::temp_dir().to_string_lossy().to_string()),
        )
        .join("protoc-cache")
    } else {
        // Unix: Use ~/.cache/protoc-cache
        let home = env::var("HOME").unwrap_or_else(|_| ".".to_string());
        PathBuf::from(home).join(".cache/protoc-cache")
    };

    Ok(cache_path)
}

fn download_and_extract_protoc(
    cache_dir: &Path,
    os: &str,
    arch: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    let (url, filename) = get_download_url(os, arch)?;
    let zip_path = cache_dir.join(&filename);

    eprintln!(
        "Downloading protoc {} from GitHub releases...",
        get_protoc_version()
    );

    if attempt_download_with_curl(&url, &zip_path).is_ok() {
        return extract_zip_file(cache_dir, &zip_path);
    }

    // Fallback: Try wget on Unix
    #[cfg(unix)]
    {
        if attempt_download_with_wget(&url, &zip_path).is_ok() {
            return extract_zip_file(cache_dir, &zip_path);
        }
    }

    // Windows fallback: PowerShell
    #[cfg(windows)]
    {
        if attempt_download_with_powershell(&url, &zip_path).is_ok() {
            return extract_zip_file(cache_dir, &zip_path);
        }
    }

    Err("Failed to download protoc. Please install 'curl' or 'protoc' manually.\n".into())
}

fn attempt_download_with_curl(url: &str, dest: &PathBuf) -> Result<(), Box<dyn std::error::Error>> {
    let status = Command::new("curl")
        .arg("-L")
        .arg("-o")
        .arg(dest)
        .arg(url)
        .status()?;

    if status.success() {
        Ok(())
    } else {
        Err("curl failed".into())
    }
}

#[cfg(unix)]
fn attempt_download_with_wget(url: &str, dest: &PathBuf) -> Result<(), Box<dyn std::error::Error>> {
    let status = Command::new("wget").arg("-O").arg(dest).arg(url).status()?;

    if status.success() {
        Ok(())
    } else {
        Err("wget failed".into())
    }
}

#[cfg(windows)]
fn attempt_download_with_powershell(
    url: &str,
    dest: &Path,
) -> Result<(), Box<dyn std::error::Error>> {
    let ps_cmd = format!(
        "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; \
         Invoke-WebRequest -Uri '{}' -OutFile '{}'",
        url,
        dest.display()
    );

    let status = Command::new("powershell")
        .arg("-NoProfile")
        .arg("-Command")
        .arg(&ps_cmd)
        .status()?;

    if status.success() {
        Ok(())
    } else {
        Err("PowerShell download failed".into())
    }
}

fn extract_zip_file(cache_dir: &Path, zip_path: &Path) -> Result<(), Box<dyn std::error::Error>> {
    eprintln!("Extracting protoc...");

    // Try unzip on Unix
    #[cfg(unix)]
    {
        if Command::new("unzip")
            .arg("-o")
            .arg(zip_path)
            .arg("-d")
            .arg(cache_dir)
            .status()?
            .success()
        {
            let _ = fs::remove_file(zip_path);
            return Ok(());
        }
    }

    // Try PowerShell on Windows
    #[cfg(windows)]
    {
        let ps_cmd = format!(
            "Expand-Archive -Path '{}' -DestinationPath '{}' -Force",
            zip_path.display(),
            cache_dir.display()
        );

        if Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(&ps_cmd)
            .status()?
            .success()
        {
            let _ = fs::remove_file(zip_path);
            return Ok(());
        }
    }

    Err("Failed to extract protoc. Please extract manually or install protoc system-wide.".into())
}

fn get_protoc_version() -> &'static str {
    "28.2"
}

fn get_download_url(os: &str, arch: &str) -> Result<(String, String), Box<dyn std::error::Error>> {
    let version = get_protoc_version();

    let (os_part, filename) = match (os, arch) {
        ("windows", "x86_64") => ("win64", format!("protoc-{}-win64.zip", version)),
        ("linux", "x86_64") => (
            "linux-x86_64",
            format!("protoc-{}-linux-x86_64.zip", version),
        ),
        ("linux", "aarch64") => (
            "linux-aarch_64",
            format!("protoc-{}-linux-aarch_64.zip", version),
        ),
        _ => return Err(format!("Unsupported platform: {} {}", os, arch).into()),
    };

    let url = format!(
        "https://github.com/protocolbuffers/protobuf/releases/download/v{}/protoc-{}-{}.zip",
        version, version, os_part
    );

    Ok((url, filename))
}

fn compile_protos() -> Result<(), Box<dyn std::error::Error>> {
    tonic_build::configure()
        .build_server(false)
        .build_client(true)
        .compile_protos(&["proto/parameters/parameters.proto"], &["proto/"])?;

    println!("cargo:rerun-if-changed=proto/parameters/parameters.proto");
    println!("cargo:rerun-if-changed=build.rs");

    Ok(())
}
