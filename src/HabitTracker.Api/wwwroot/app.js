const list = document.querySelector('#habits');
const empty = document.querySelector('#empty');
const errorBox = document.querySelector('#error');
const dialog = document.querySelector('#dialog');
const nameInput = document.querySelector('#name');

// Fecha local (yyyy-MM-dd) para no depender de la zona horaria del servidor.
const iso = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const todayIso = () => iso(new Date());

document.querySelector('#today').textContent =
  new Date().toLocaleDateString('es', { weekday: 'long', day: 'numeric', month: 'long' });

async function request(url, options) {
  const response = await fetch(url, options);
  if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error ?? `Error ${response.status}`);
  return response.status === 204 ? null : response.json();
}

function showError(message) {
  errorBox.textContent = message ?? '';
  errorBox.hidden = !message;
}

// fetch falla con TypeError cuando no hay conexión con el servidor ("Failed to fetch" o "Load failed").
const isUnreachable = (e) => e instanceof TypeError;

function messageFor(e) {
  if (!isUnreachable(e)) return e.message;
  return navigator.onLine
    ? 'No se pudo conectar con el servidor. Reintentando…'
    : 'Sin conexión: no se pueden cargar los hábitos.';
}

function render(habits) {
  list.replaceChildren(...habits.map(renderHabit));
  empty.hidden = habits.length > 0;
}

function renderHabit(habit) {
  const doneToday = habit.last7Days[6];
  const li = document.createElement('li');

  const check = document.createElement('button');
  check.className = 'check';
  check.textContent = '✓';
  check.setAttribute('aria-pressed', String(doneToday));
  check.setAttribute('aria-label', `Marcar ${habit.name} como hecho hoy`);
  check.addEventListener('click', () => toggle(habit, doneToday));

  const info = document.createElement('div');
  info.className = 'info';
  const title = document.createElement('strong');
  title.textContent = habit.name;
  const streak = document.createElement('span');
  streak.className = 'streak';
  streak.textContent = `🔥 ${habit.currentStreak} · mejor ${habit.bestStreak}`;
  const week = document.createElement('div');
  week.className = 'week';
  for (const on of habit.last7Days) {
    const dot = document.createElement('span');
    if (on) dot.className = 'on';
    week.append(dot);
  }
  info.append(title, streak, week);

  const del = document.createElement('button');
  del.className = 'del';
  del.textContent = '🗑';
  del.setAttribute('aria-label', `Eliminar ${habit.name}`);
  del.addEventListener('click', () => remove(habit));

  li.append(check, info, del);
  return li;
}

let retryTimer;

async function load() {
  clearTimeout(retryTimer);
  try {
    render(await request(`/api/habits?today=${todayIso()}`));
    showError();
  } catch (e) {
    showError(messageFor(e));
    // La interfaz puede abrirse desde la caché antes de que el servidor esté listo: reintenta solo.
    if (isUnreachable(e)) retryTimer = setTimeout(load, 3000);
  }
}

async function toggle(habit, done) {
  try {
    await request(`/api/habits/${habit.id}/checkins/${todayIso()}`, { method: done ? 'DELETE' : 'PUT' });
    await load();
  } catch (e) { showError(messageFor(e)); }
}

async function remove(habit) {
  if (!confirm(`¿Eliminar "${habit.name}" y todos sus registros?`)) return;
  try {
    await request(`/api/habits/${habit.id}`, { method: 'DELETE' });
    await load();
  } catch (e) { showError(messageFor(e)); }
}

document.querySelector('#add').addEventListener('click', () => { nameInput.value = ''; dialog.showModal(); nameInput.focus(); });
document.querySelector('#cancel').addEventListener('click', () => dialog.close());
document.querySelector('#form').addEventListener('submit', async (event) => {
  const name = nameInput.value.trim();
  if (!name) { event.preventDefault(); return; }
  try {
    await request('/api/habits', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name }) });
    await load();
  } catch (e) { showError(messageFor(e)); }
});

if ('serviceWorker' in navigator) navigator.serviceWorker.register('sw.js');
load();
