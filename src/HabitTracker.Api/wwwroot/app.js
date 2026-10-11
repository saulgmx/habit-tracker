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

// Estado en memoria: marcar un hábito lo cambia aquí al instante y el servidor lo confirma después.
let habits = [];
const byId = (id) => habits.find((h) => h.id === id);

function render() {
  const focusedId = document.activeElement?.dataset?.id;
  list.replaceChildren(...habits.map(renderHabit));
  empty.hidden = habits.length > 0;
  // Reconstruir la lista quita el foco: se devuelve al botón que se estaba usando (teclado y lectores de pantalla).
  if (focusedId) list.querySelector(`.check[data-id="${focusedId}"]`)?.focus();
}

function renderHabit(habit) {
  const doneToday = habit.last7Days[6];
  const li = document.createElement('li');

  const check = document.createElement('button');
  check.className = 'check';
  check.textContent = '✓';
  check.setAttribute('aria-pressed', String(doneToday));
  check.setAttribute('aria-label', `Marcar ${habit.name} como hecho hoy`);
  check.dataset.id = habit.id;
  check.addEventListener('click', () => toggle(habit.id));

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

// keepError: conserva el mensaje que ya se muestra (por ejemplo, el de un cambio que el servidor rechazó).
async function load({ keepError = false } = {}) {
  clearTimeout(retryTimer);
  try {
    const fresh = await request(`/api/habits?today=${todayIso()}`);
    // Un hábito con cambios sin confirmar conserva su versión local: la lista pudo pedirse antes de que el servidor los aplicara.
    habits = fresh.map((h) => ((pending.get(h.id) ?? 0) > 0 ? byId(h.id) ?? h : h));
    render();
    if (!keepError) showError();
  } catch (e) {
    showError(messageFor(e));
    // La interfaz puede abrirse desde la caché antes de que el servidor esté listo: reintenta solo.
    if (isUnreachable(e)) retryTimer = setTimeout(load, 3000);
  }
}

const pending = new Map(); // id -> cambios sin confirmar por el servidor
const queues = new Map();  // id -> cola de peticiones: salen en orden, una a la vez por hábito

// Versión local del hábito tras marcar o desmarcar HOY. La racha actual sube o baja 1 (hoy cuenta o deja de contar);
// la máxima solo puede subir: al desmarcar queda como estaba hasta que el servidor confirme el valor real.
function toggled(habit) {
  const wasDone = habit.last7Days[6];
  const last7Days = habit.last7Days.slice();
  last7Days[6] = !wasDone;
  const currentStreak = Math.max(0, habit.currentStreak + (wasDone ? -1 : 1));
  const bestStreak = wasDone ? habit.bestStreak : Math.max(habit.bestStreak, currentStreak);
  return { ...habit, last7Days, currentStreak, bestStreak };
}

function toggle(id) {
  const habit = byId(id);
  if (!habit) return;

  const wasDone = habit.last7Days[6];
  habits = habits.map((h) => (h.id === id ? toggled(h) : h));
  render(); // la pantalla cambia sin esperar al servidor

  pending.set(id, (pending.get(id) ?? 0) + 1);
  const today = todayIso();
  const send = () => request(`/api/habits/${id}/checkins/${today}?today=${today}`, { method: wasDone ? 'DELETE' : 'PUT' });
  queues.set(id, (queues.get(id) ?? Promise.resolve()).then(send).then(
    (updated) => settle(id, updated),
    (e) => settle(id, null, e)));
}

// Al terminar la última petición pendiente del hábito, el servidor manda: si respondió, se usa su versión
// (con las rachas reales); si falló, se muestra el error y se recarga la lista para revertir el cambio local.
function settle(id, updated, error) {
  const left = pending.get(id) - 1;
  pending.set(id, left);
  if (error) showError(messageFor(error));
  if (left > 0) return;
  if (error) return load({ keepError: true });
  habits = habits.map((h) => (h.id === id ? updated : h));
  render();
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
