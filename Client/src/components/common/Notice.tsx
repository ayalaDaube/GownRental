import './Notice.css';

interface NoticeProps {
  type: 'error' | 'success';
  text: string;
  onClose: () => void;
}

/** A dismissible message shown after an action, for example "this gown cannot be deleted". */
function Notice({ type, text, onClose }: NoticeProps) {
  return (
    <div className={`notice notice-${type}`} role={type === 'error' ? 'alert' : 'status'}>
      <span>{text}</span>
      <button type="button" className="notice-close" aria-label="Dismiss message" onClick={onClose}>
        ×
      </button>
    </div>
  );
}

export default Notice;
