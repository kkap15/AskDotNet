import { useEffect } from 'react';
import { Header } from './components/Header';
import { MessageBubble } from './components/MessageBubble';
import { SourcesList } from './components/SourcesList';
import { InputBar } from './components/InputBar';
import { useChat } from './hooks/useChat';

export default function App() {
  const { messages, sources, currentInput, setCurrentInput, isThinking, endRef, handleSubmit } = useChat();

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, endRef]);

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Geist:wght@300;400;500;600&family=Geist+Mono:wght@400;500&display=swap');
        * { font-family: 'Geist', system-ui, sans-serif; }
        code, pre { font-family: 'Geist Mono', monospace !important; }
        @keyframes blink { 0%, 100% { opacity: 1; } 50% { opacity: 0; } }
        @keyframes fadeUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
        .message-enter { animation: fadeUp 0.2s ease-out forwards; }
        .prose-dark p { margin: 0.5em 0; line-height: 1.65; }
        .prose-dark p:first-child { margin-top: 0; }
        .prose-dark p:last-child { margin-bottom: 0; }
        .prose-dark code { background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.1); border-radius: 4px; padding: 1px 5px; font-size: 0.85em; }
        .prose-dark pre { background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.08); border-radius: 8px; padding: 14px 16px; overflow-x: auto; margin: 10px 0; }
        .prose-dark pre code { background: none; border: none; padding: 0; font-size: 0.88em; }
        .prose-dark ul, .prose-dark ol { padding-left: 1.4em; margin: 0.5em 0; }
        .prose-dark li { margin: 0.25em 0; }
        .prose-dark a { color: #60a5fa; text-decoration: underline; text-underline-offset: 2px; }
        .prose-user p { margin: 0; line-height: 1.6; }
        .prose-user code { background: rgba(255,255,255,0.15); border-radius: 3px; padding: 1px 5px; font-size: 0.85em; }
        ::-webkit-scrollbar { width: 4px; }
        ::-webkit-scrollbar-track { background: transparent; }
        ::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.1); border-radius: 2px; }
      `}</style>

      <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', background: '#0a0a0a', color: '#e5e5e5' }}>
        <Header />

        <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', padding: '32px 24px' }}>
          <div style={{ maxWidth: '680px', margin: '0 auto', display: 'flex', flexDirection: 'column', gap: '4px' }}>
            {messages.map((message) => (
              <div key={message.id} className="message-enter">
                <MessageBubble message={message} />
              </div>
            ))}
            <SourcesList sources={sources} />
            <div ref={endRef} />
          </div>
        </div>

        <InputBar
          currentInput={currentInput}
          setCurrentInput={setCurrentInput}
          isThinking={isThinking}
          handleSubmit={handleSubmit}
        />
      </div>
    </>
  );
}