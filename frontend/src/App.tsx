import { useEffect, useRef, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import ReactMarkdown from "react-markdown";

type Message = {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  status: 'thinking' | 'complete'
  timestamp: Date;
}

type ChunkReference = {
  SourceUrl: string;
  SourceTitle: string;
  SectionHeading: string;
  Similarity: number;
}

function App() {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: "Hi! Ask me anything.",
      status: 'complete',
      timestamp: new Date()
    },
  ]);
  
  const { isAuthenticated, loginWithRedirect, getAccessTokenSilently } = useAuth0();
  const [currentInput, setCurrentInput] = useState('');
  const [sources, setSources] = useState<ChunkReference[]>([]);
  const isThinking = messages.some(m => m.status === 'thinking');
  const endRef = useRef<HTMLDivElement>(null);
  
  const handleSubmit = async () => {
    setSources([]);
    if (!currentInput.trim() || isThinking) return;
    const newMessage : Message = {
      id: crypto.randomUUID(),
      role: 'user',
      status: 'complete',
      content: currentInput,
      timestamp: new Date()
    };
    const placeholder: Message = {
      id: crypto.randomUUID(),
      role: 'assistant',
      status: "thinking",
      content: '',
      timestamp: new Date()
    }
    setMessages([...messages, newMessage, placeholder]);
    const placeholderId = placeholder.id;
    const userContent = currentInput;
    setCurrentInput('');
    
    try {
      const token = await getAccessTokenSilently({
        authorizationParams: {audience: import.meta.env.VITE_AUTH0_AUDIENCE},
      });
      const response = await fetch(`${import.meta.env.VITE_API_URL}/api/chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({question: userContent}),
      });
      
      if (!response.ok) {
        throw new Error(`Failed to fetch data ${response.status}`);
      }
      if (!response.body) throw new Error(`No response body`);
      
      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      let isSource = false;
      
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';
        
        for (const line of lines) {
          if (line === 'event: sources') {
            isSource = true;
            continue;
          }
          if (line.startsWith('data: ')) {
            const token = line.slice(6);
            if (isSource) {
              setSources(JSON.parse(token) as ChunkReference[]);
              console.log('sources: ', JSON.parse(token));
              isSource = false;
            } else {
              setMessages(prev => prev.map(msg =>
                  msg.id === placeholderId
                      ? { ...msg, content: msg.content + token, status: 'thinking' }
                      : msg
              ));
            }
          }
        }
      }
      setMessages(prev => prev.map(msg =>
          msg.id === placeholderId
              ? { ...msg, status: 'complete' }
              : msg
      ));
    } catch (err) {
      setMessages(prev => prev.map(msg =>
        msg.id === placeholderId
          ? { ...msg, content: 'Something went wrong. Please try again.', status: 'complete' }
          : msg
      ));
    }    
  }

  useEffect(() => {
    endRef.current?.scrollIntoView({behavior: "smooth"});
  }, [messages]);

  return(
      <div className="flex flex-col h-screen bg-gray-50">
        <header className="bg-slate-800 text-white px-6 py-4 shadow-sm">
          <h1 className="text-xl font-semibold">AskDotNet</h1>
        </header>

        <div className="flex-1 min-h-0 overflow-y-auto px-4 py-6">
          <div className="max-w-2xl mx-auto flex flex-col gap-3">
            {messages.map((message) => (
                <div key={message.id} className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                  <div className={`max-w-md px-4 py-2 rounded-lg ${message.role === 'user' ? 'bg-blue-500 text-white': 'bg-gray-200 text-gray-900'}`}
                  >
                    {message.status === 'thinking' && message.content === '' ? (
                        <span className="animate-pulse italic text-gray-500">...</span>
                    ) : (
                        <ReactMarkdown>{message.content}</ReactMarkdown>
                    )}
                  </div>
                </div>
            ))}
            {sources.length > 0 && (
                <div className="max-w-2xl mx-auto mt-4 p-4 bg-white rounded-lg border border-gray-200">
                  <h3 className="text-sm font-semibold text-gray-500 mb-2">Sources</h3>
                  <ul className="space-y-2">
                    {sources.map((source, index) => (
                        <li key={index} className="text-sm">
                          <a href={source.SourceUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-500 hover:underline font-medium" 
                          >
                            {source.SourceTitle} - {source.SectionHeading}
                          </a>
                          <span className="text-gray-400 ml-2">
                            {Math.round(source.Similarity * 100)} % match
                          </span>
                        </li>
                        )
                    )}
                  </ul>
                </div>
            )}
            <div ref={endRef} />
          </div>
        </div>
        {!isAuthenticated ? (
            <div className="border-t bg-white px-4 py-3">
              <div className="max-w-2xl mx-auto flex justify-center">
                <button onClick={() => loginWithRedirect()} className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">Sign in to chat</button>
              </div>
            </div>
          ) : (
            <div className="border-t bg-white px-4 py-3">
              <div className="max-w-2xl mx-auto flex gap-2">
                <input
                    type="text"
                    value={currentInput}
                    onChange={(e) => setCurrentInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && void handleSubmit()}
                    disabled={isThinking}
                    placeholder="Type a message...."
                    className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
                />
                <button
                    disabled={!currentInput.trim() || isThinking}
                    onClick={handleSubmit}
                    className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:bg-gray-300 disabled:cursor-not-allowed"
                >
                  Send
                </button>
              </div>
            </div>
          )
        }
      </div>
  );
}

export default App;