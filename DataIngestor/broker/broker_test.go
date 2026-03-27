package broker

import (
	"errors"
	"fmt"
	"reflect"
	"testing"
	"time"

	"github.com/IBM/sarama"
)

func fastFailSaramaConfig() *sarama.Config {
	cfg := sarama.NewConfig()
	cfg.Producer.RequiredAcks = sarama.WaitForAll
	cfg.Producer.Retry.Max = 1
	cfg.Producer.Return.Successes = true
	cfg.Net.DialTimeout = 100 * time.Millisecond
	cfg.Net.ReadTimeout = 100 * time.Millisecond
	cfg.Net.WriteTimeout = 100 * time.Millisecond
	return cfg
}

type fakeProducer struct {
	lastTopic string
	lastValue []byte
	sendCount int
	fail      bool
}

func (p *fakeProducer) SendMessage(msg *sarama.ProducerMessage) (partition int32, offset int64, err error) {
	if msg == nil {
		return 0, 0, fmt.Errorf("nil message")
	}
	if msg.Value != nil {
		if be, ok := msg.Value.(sarama.ByteEncoder); ok {
			p.lastValue = []byte(be)
		} else {
			p.lastValue = nil
		}
	} else {
		p.lastValue = nil
	}

	p.lastTopic = msg.Topic
	p.sendCount++
	if p.fail {
		return 0, 0, fmt.Errorf("producer error")
	}
	return 1, int64(p.sendCount), nil
}

func (p *fakeProducer) SendMessages(msgs []*sarama.ProducerMessage) error {
	if len(msgs) == 0 {
		return nil
	}

	last := msgs[len(msgs)-1]
	if last != nil && last.Value != nil {
		if be, ok := last.Value.(sarama.ByteEncoder); ok {
			p.lastValue = []byte(be)
		} else {
			p.lastValue = nil
		}
		p.lastTopic = last.Topic
		p.sendCount += len(msgs)
	}
	return nil
}

func (p *fakeProducer) Close() error { return nil }

func TestSendToKafka(t *testing.T) {
	fp := &fakeProducer{}
	kp := &KafkaProducer{
		producer: fp,
		topic:    "test-topic",
	}

	payload := []byte(`{"hello":"world"}`)
	partition, offset, err := kp.Send(&Message{Value: payload})
	if err != nil {
		t.Fatalf("Send returned error: %v", err)
	}
	if partition != 1 {
		t.Errorf("expected partition 1, got %d", partition)
	}
	if offset != int64(fp.sendCount) {
		t.Errorf("expected offset %d, got %d", fp.sendCount, offset)
	}
	if fp.lastTopic != "test-topic" {
		t.Errorf("expected topic 'test-topic', got '%s'", fp.lastTopic)
	}
	if string(fp.lastValue) != string(payload) {
		t.Errorf("expected value '%s', got '%s'", string(payload), string(fp.lastValue))
	}
}

func TestSendMessage(t *testing.T) {
	fp := &fakeProducer{}
	kp := &KafkaProducer{
		producer: fp,
		topic:    "topic-msg",
	}
	msg := &sarama.ProducerMessage{
		Topic: "topic-msg",
		Value: sarama.ByteEncoder([]byte("abc")),
	}
	partition, offset, err := kp.SendMessage(msg)
	if err != nil {
		t.Fatalf("SendMessage returned error: %v", err)
	}
	if partition != 1 {
		t.Errorf("expected partition 1, got %d", partition)
	}
	if offset != int64(fp.sendCount) {
		t.Errorf("expected offset %d, got %d", fp.sendCount, offset)
	}
	if fp.lastTopic != "topic-msg" {
		t.Errorf("expected topic 'topic-msg', got '%s'", fp.lastTopic)
	}
	if string(fp.lastValue) != "abc" {
		t.Errorf("expected value 'abc', got '%s'", string(fp.lastValue))
	}
}

func TestSendMessages(t *testing.T) {
	fp := &fakeProducer{}
	kp := &KafkaProducer{
		producer: fp,
		topic:    "topic-batch",
	}
	msgs := []*sarama.ProducerMessage{
		{
			Topic: "topic-batch",
			Value: sarama.ByteEncoder([]byte("one")),
		},
		{
			Topic: "topic-batch",
			Value: sarama.ByteEncoder([]byte("two")),
		},
	}
	err := kp.SendMessages(msgs)
	if err != nil {
		t.Fatalf("SendMessages returned error: %v", err)
	}
	if fp.sendCount != 2 {
		t.Errorf("expected sendCount 2, got %d", fp.sendCount)
	}
	if fp.lastTopic != "topic-batch" {
		t.Errorf("expected topic 'topic-batch', got '%s'", fp.lastTopic)
	}
	if string(fp.lastValue) != "two" {
		t.Errorf("expected last value 'two', got '%s'", string(fp.lastValue))
	}
}

func TestKafkaProducer_Close(t *testing.T) {
	fp := &fakeProducer{}
	kp := &KafkaProducer{
		producer: fp,
		topic:    "topic-close",
	}
	err := kp.Close()
	if err != nil {
		t.Errorf("expected nil error from Close, got %v", err)
	}
}

type errProducer struct{}

func (e *errProducer) SendMessage(msg *sarama.ProducerMessage) (int32, int64, error) {
	return 0, 0, errors.New("fail send")
}
func (e *errProducer) SendMessages(msgs []*sarama.ProducerMessage) error {
	return errors.New("fail send batch")
}
func (e *errProducer) Close() error {
	return errors.New("fail close")
}

func TestKafkaProducer_ErrorCases(t *testing.T) {
	ep := &errProducer{}
	kp := &KafkaProducer{
		producer: ep,
		topic:    "topic-err",
	}
	_, _, err := kp.SendMessage(&sarama.ProducerMessage{Topic: "topic-err"})
	if err == nil {
		t.Error("expected error from SendMessage, got nil")
	}
	err = kp.SendMessages([]*sarama.ProducerMessage{{Topic: "topic-err"}})
	if err == nil {
		t.Error("expected error from SendMessages, got nil")
	}
	err = kp.Close()
	if err == nil {
		t.Error("expected error from Close, got nil")
	}
}

func TestNewKafkaProducer_Error(t *testing.T) {
	cfg := fastFailSaramaConfig()
	_, err := NewKafkaProducer([]string{"invalid:9092"}, "topic", cfg)
	if err == nil {
		t.Error("expected error from NewKafkaProducer with invalid broker, got nil")
	}
}

func TestDefaultSaramaConfig(t *testing.T) {
	cfg := SaramaConfig(2)
	if cfg.Producer.RequiredAcks != sarama.WaitForAll {
		t.Errorf("Expected RequiredAcks WaitForAll, got %v", cfg.Producer.RequiredAcks)
	}
	if cfg.Producer.Retry.Max != 2 {
		t.Errorf("Expected Retry.Max 2, got %v", cfg.Producer.Retry.Max)
	}
	if !cfg.Producer.Return.Successes {
		t.Errorf("Expected Return.Successes true, got false")
	}

	if reflect.TypeFor[*sarama.Config]().String() != "*sarama.Config" {
		t.Errorf("Expected type *sarama.Config, got %T", cfg)
	}
}

func TestSendToKafka_Failure(t *testing.T) {
	fp := &fakeProducer{fail: true}
	kp := &KafkaProducer{
		producer: fp,
		topic:    "topic-x",
	}

	_, _, err := kp.Send(&Message{Value: []byte(`{"x":1}`)})
	if err == nil {
		t.Fatalf("expected error from Send when producer fails, got nil")
	}
}
