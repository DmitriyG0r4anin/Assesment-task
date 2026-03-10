package models

import (
	"encoding/json"
	"time"
)

type ResponseItem struct {
	Type    string          `json:"type"`
	Name    string          `json:"name"`
	Payload json.RawMessage `json:"payload"`
}

type MeterData struct {
	Data      json.RawMessage `json:"data"`
	Timestamp time.Time       `json:"timestamp"`
}
