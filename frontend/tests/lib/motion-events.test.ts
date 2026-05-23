import {
    clearMotionEvents,
    getMotionEventsSnapshot,
    pushMotionEvent,
    subscribeMotionEvents,
} from '@/lib/motion-events';
import type { MotionEvent } from '@/types';

const sampleEvent = (id: number): MotionEvent => ({
    roomName: `Room ${id}`,
    isDetected: id % 2 === 0,
    timestamp: `2024-06-01T12:00:0${id}Z`,
});

describe('motion-events store', () => {
    beforeEach(() => {
        clearMotionEvents();
    });

    it('notifies subscribers when events are pushed', () => {
        const listener = vi.fn();
        const unsubscribe = subscribeMotionEvents(listener);

        pushMotionEvent(sampleEvent(1));

        expect(listener).toHaveBeenCalledTimes(1);
        expect(getMotionEventsSnapshot()).toHaveLength(1);

        unsubscribe();
    });

    it('prepends events and caps the list at 100 items', () => {
        for (let index = 0; index < 101; index += 1) {
            pushMotionEvent(sampleEvent(index));
        }

        const snapshot = getMotionEventsSnapshot();
        expect(snapshot).toHaveLength(100);
        expect(snapshot[0]?.roomName).toBe('Room 100');
    });

    it('clears events and notifies subscribers only when needed', () => {
        const listener = vi.fn();
        subscribeMotionEvents(listener);

        clearMotionEvents();
        expect(listener).not.toHaveBeenCalled();

        pushMotionEvent(sampleEvent(1));
        clearMotionEvents();

        expect(getMotionEventsSnapshot()).toEqual([]);
        expect(listener).toHaveBeenCalledTimes(2);
    });
});
