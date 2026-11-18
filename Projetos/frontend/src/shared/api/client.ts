export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}
let csrfToken: string | undefined;
let unauthorized: (() => void) | undefined;
export function onUnauthorized(callback: () => void) { unauthorized = callback; return () => { if (unauthorized === callback) unauthorized = undefined; }; }
export function resetCsrf() { csrfToken = undefined; }
export async function refreshCsrf(): Promise<void> {
  const response = await fetch('/api/v1/auth/csrf', { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' } });
  if (!response.ok) throw new ApiError(response.status, 'Não foi possível preparar a sessão. Tente novamente.');
  csrfToken = (await response.json() as { token: string }).token;
}
export async function request<T>(path: string, options: { method?: string; body?: unknown; signal?: AbortSignal } = {}): Promise<T> {
  if (!path.startsWith('/api/v1/')) throw new Error('Endereço de API inválido.');
  const method = options.method ?? 'GET';
  const mutation = !['GET', 'HEAD', 'OPTIONS'].includes(method);
  if (mutation && !csrfToken) await refreshCsrf();
  const isFormData = typeof FormData !== 'undefined' && options.body instanceof FormData;
  const response = await fetch(path, {
    method, signal: options.signal, credentials: 'same-origin', cache: 'no-store',
    headers: { Accept: 'application/json', ...(!isFormData && options.body !== undefined ? { 'Content-Type': 'application/json' } : {}), ...(mutation ? { 'X-CSRF-TOKEN': csrfToken! } : {}) },
    body: options.body === undefined ? undefined : isFormData ? options.body as FormData : JSON.stringify(options.body),
  });
  if (!response.ok) {
    if (response.status === 401) { resetCsrf(); unauthorized?.(); }
    const problem = await response.json().catch(() => ({})) as { detail?: string; title?: string; errors?: Record<string, string[]> };
    const translations: Record<string, string> = { DuplicateEmail: 'Já existe uma conta com esse email.', DuplicateUserName: 'Já existe uma conta com esse email.', PasswordTooShort: 'A senha precisa ter ao menos 10 caracteres.', PasswordRequiresNonAlphanumeric: 'Inclua um símbolo na senha.', PasswordRequiresDigit: 'Inclua um número na senha.', PasswordRequiresUpper: 'Inclua uma letra maiúscula na senha.', PasswordRequiresLower: 'Inclua uma letra minúscula na senha.' };
    const errors = problem.errors ? Object.entries(problem.errors).flatMap(([key, values]) => translations[key] ? [translations[key]] : values).join(' ') : '';
    throw new ApiError(response.status, problem.detail || errors || (response.status === 401 ? 'Email, senha ou código não conferem. Verifique e tente novamente.' : response.status >= 500 ? 'O serviço não respondeu. Tente novamente.' : response.status === 403 ? 'Você não tem permissão para esta ação no espaço ativo.' : response.status === 404 ? 'Cadastro não encontrado no espaço ativo.' : problem.title) || 'Não foi possível concluir a operação.');
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}
export function requestUpload<T>(path: string, file: File, signal?: AbortSignal) {
  const body = new FormData();
  body.append('file', file, file.name);
  return request<T>(path, { method: 'POST', body, signal });
}
export const get = <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal });
