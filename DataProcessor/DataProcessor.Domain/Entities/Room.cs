using DataProcessor.Domain.Entities.Base;

namespace DataProcessor.Domain.Entities;

public class Room : BaseEntity
{
    public required string Name { get; set; }
}
