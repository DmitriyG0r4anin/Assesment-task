package models

import (
	"time"
)

type OutgoingMessage struct {
	Type      string    `json:"type"`
	Name      string    `json:"name"`
	Payload   any       `json:"payload"`
	Timestamp time.Time `json:"timestamp"`
}

type AirQualityPayload struct {
	CO2      int `json:"co2"`
	PM25     int `json:"pm25"`
	Humidity int `json:"humidity"`
}

type MotionPayload struct {
	MotionDetected bool `json:"motionDetected"`
}

type EnergyPayload struct {
	Energy float32 `json:"energy"`
}
