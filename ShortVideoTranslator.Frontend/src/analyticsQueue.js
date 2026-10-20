// No coordinates, account data or free text enter this queue.
export class EventQueue {
  constructor({ read, write, send, now = Date.now }) { Object.assign(this, { read, write, send, now }); this.busy = false; this.retryAt = 0; }
  add(event) {
    const items = this.read().filter(e => this.now() - Date.parse(e.occurredAt) < 86400000);
    if (event.kind === 'heartbeat' && items.some(e => e.kind === 'heartbeat')) return;
    this.write([...items, event].slice(-100));
  }
  clear() { this.write([]); }
  async flush(allowed) {
    if (this.busy || !allowed() || this.now() < this.retryAt) return;
    const batch = this.read().filter(e => this.now() - Date.parse(e.occurredAt) < 86400000).slice(0, 25);
    if (!batch.length) return;
    this.busy = true;
    try {
      await this.send(batch);
      const ids = new Set(batch.map(e => e.id)); this.write(this.read().filter(e => !ids.has(e.id)));
      this.retryAt = 0;
    } catch (error) {
      if ([400, 404, 409].includes(error.status)) {
        const ids = new Set(batch.map(e => e.id)); this.write(this.read().filter(e => !ids.has(e.id)));
      }
      this.retryAt = this.now() + 60000;
    } finally { this.busy = false; }
  }
}
