package broker

import (
	"fmt"
	"time"

	"github.com/IBM/sarama"
)

type Producer interface {
	SendMessage(msg *sarama.ProducerMessage) (partition int32, offset int64, err error)
	SendMessages(msgs []*sarama.ProducerMessage) error
	Close() error
	// Send is a convenience method for sending a simple message.
	Send(msg *Message) (partition int32, offset int64, err error)
}

type syncProducerInterface interface {
	SendMessage(msg *sarama.ProducerMessage) (partition int32, offset int64, err error)
	SendMessages(msgs []*sarama.ProducerMessage) error
	Close() error
}

type KafkaProducer struct {
	producer syncProducerInterface
	topic    string
}

func NewKafkaProducer(brokers []string, topic string, config *sarama.Config) (*KafkaProducer, error) {
	producer, err := sarama.NewSyncProducer(brokers, config)
	if err != nil {
		return nil, fmt.Errorf("failed to create Kafka producer: %w", err)
	}
	return &KafkaProducer{
		producer: producer,
		topic:    topic,
	}, nil
}

type Message struct {
	Value []byte
}

func (kp *KafkaProducer) Send(msg *Message) (partition int32, offset int64, err error) {
	message := &sarama.ProducerMessage{
		Topic:     kp.topic,
		Value:     sarama.ByteEncoder(msg.Value),
		Timestamp: time.Now(),
	}
	return kp.producer.SendMessage(message)
}

func (kp *KafkaProducer) SendMessage(msg *sarama.ProducerMessage) (partition int32, offset int64, err error) {
	return kp.producer.SendMessage(msg)
}

func (kp *KafkaProducer) SendMessages(msgs []*sarama.ProducerMessage) error {
	return kp.producer.SendMessages(msgs)
}

func (kp *KafkaProducer) Close() error {
	return kp.producer.Close()
}

func DefaultSaramaConfig() *sarama.Config {
	cfg := sarama.NewConfig()
	cfg.Producer.RequiredAcks = sarama.WaitForAll
	cfg.Producer.Retry.Max = 5
	cfg.Producer.Return.Successes = true
	return cfg
}
