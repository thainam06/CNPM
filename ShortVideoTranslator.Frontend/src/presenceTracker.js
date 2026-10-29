// One session per visible page. Multiple tabs still share a browser ID.
export function startPresenceTracking({ createSession, sendHeartbeat, sendLeave, isVisible,
  subscribeVisibility, subscribePageHide, subscribePageShow, subscribeOnline,
  schedule = (fn, ms) => setTimeout(fn, ms), cancel = id => clearTimeout(id) }) {
  let session; let timer; let stopped = false; const inFlight = new Set();
  const depart = () => {
    cancel(timer);
    if (session) { const departing = session; session = null; sendLeave(departing); }
  };
  const tick = async () => {
    if (stopped || !isVisible()) return;
    cancel(timer);
    session ||= createSession(); const current = session;
    if (inFlight.has(current)) return;
    inFlight.add(current);
    try { await sendHeartbeat(current); } catch { /* Presence must not block the site. */ }
    inFlight.delete(current);
    /// gửi hearbeat sau bao nhiêu giây
    if (!stopped && session === current && isVisible()) timer = schedule(tick, 15000);
  };
  const visibility = () => { if (isVisible()) tick(); else depart(); };
  const remove = [subscribeVisibility(visibility), subscribePageHide(depart), subscribePageShow(visibility), subscribeOnline(tick)];
  tick();
  return () => { stopped = true; depart(); remove.forEach(unsubscribe => unsubscribe()); };
}
