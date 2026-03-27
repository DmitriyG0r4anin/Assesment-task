package broker

import (
	"fmt"
	"testing"

	"github.com/IBM/sarama"
)

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
