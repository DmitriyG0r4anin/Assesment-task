import type { MotionEvent } from "../types";

const MAX_EVENTS = 100;

let events: readonly MotionEvent[] = [];
const listeners = new Set<() => void>();

function notify(): void {
  listeners.forEach((listener) => listener());
}

export function subscribeMotionEvents(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function getMotionEventsSnapshot(): readonly MotionEvent[] {
  return events;
}

export function pushMotionEvent(event: MotionEvent): void {
  const next = [event, ...events];
  events = next.length > MAX_EVENTS ? next.slice(0, MAX_EVENTS) : next;
  notify();
}

export function clearMotionEvents(): void {
  if (events.length === 0) return;
  events = [];
  notify();
}
