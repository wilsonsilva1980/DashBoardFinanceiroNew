import { useEffect, useId, useRef, type ReactNode } from 'react';

export function Dialog({ title, onClose, children }: { title: string; onClose(): void; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null);
  const id = useId();
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null;
    ref.current?.showModal();
    return () => { ref.current?.close(); previous?.focus(); };
  }, []);
  return <dialog ref={ref} aria-labelledby={id} onCancel={event => { event.preventDefault(); onClose(); }}>
    <div className="dialog-heading"><div><p className="eyebrow">Seu espaço financeiro</p><h2 id={id}>{title}</h2></div><button type="button" className="icon-button" aria-label="Fechar formulário" onClick={onClose}>×</button></div>
    {children}
  </dialog>;
}
