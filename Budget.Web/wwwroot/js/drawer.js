// Drawer state management with localStorage persistence
export function initialize() {
    const storageKey = 'mudblazor-drawer-open';
    let isOpen = localStorage.getItem(storageKey) !== 'false'; // default true

    window.mudDrawer = {
        isOpen: () => isOpen,
        
        toggle: () => {
            isOpen = !isOpen;
            localStorage.setItem(storageKey, isOpen.toString());
            window.dispatchEvent(new CustomEvent('drawer-state-changed', { detail: isOpen }));
            return isOpen;
        },

        setOpen: (value) => {
            if (isOpen !== value) {
                isOpen = value;
                localStorage.setItem(storageKey, isOpen.toString());
                window.dispatchEvent(new CustomEvent('drawer-state-changed', { detail: isOpen }));
            }
        }
    };
}
