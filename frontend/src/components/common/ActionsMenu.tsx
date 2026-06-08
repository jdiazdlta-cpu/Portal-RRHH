import { useEffect, useRef, useState } from 'react';

export type ActionMenuItem = {
  label: string;
  onClick: () => void;
  disabled?: boolean;
};

type ActionsMenuProps = {
  items: ActionMenuItem[];
};

export function ActionsMenu({ items }: ActionsMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const handleClick = (event: MouseEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClick);

    return () => {
      document.removeEventListener('mousedown', handleClick);
    };
  }, [isOpen]);

  return (
    <div className="actions-menu" ref={menuRef}>
      <button
        aria-expanded={isOpen}
        aria-haspopup="menu"
        className="actions-menu-trigger"
        type="button"
        onClick={() => setIsOpen((current) => !current)}
      >
        Acciones
        <span aria-hidden="true">v</span>
      </button>

      {isOpen && (
        <div className="actions-menu-list" role="menu">
          {items.map((item) => (
            <button
              disabled={item.disabled}
              key={item.label}
              role="menuitem"
              type="button"
              onClick={() => {
                item.onClick();
                setIsOpen(false);
              }}
            >
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
