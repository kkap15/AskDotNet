import { useRef, useState } from "react";
import type { Message } from "../types/Message";
import type { ChunkReference } from "../types/ChunkReference";
import { useAuth0 } from "@auth0/auth0-react";
import { streamChat } from "../services/chatApi";

export function useChat() {
    const [messages, setMessages] = useState<Message[]>([
        {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: 'Hi! Ask me anything about C#.',
            status: 'complete',
            timestamp: new Date()
        },
    ]);
    const [sources, setSources] = useState<ChunkReference[]>([]);
    const [currentInput, setCurrentInput] = useState('');
    const endRef = useRef<HTMLDivElement>(null);
    const { getAccessTokenSilently } = useAuth0();
    
    const isThinking = messages.some(m => m.status === 'thinking');
    
    const handleSubmit = async () => {
        setSources([]);
        if (!currentInput.trim() || isThinking) return;
        
        const newMessage: Message = {
            id: crypto.randomUUID(),
            role: 'user',
            status: 'complete',
            content: currentInput,
            timestamp: new Date()
        };
        
        const placeholder: Message = {
            id: crypto.randomUUID(), 
            role: 'assistant',
            status: 'thinking',
            content: '',
            timestamp: new Date()
        };
        
        setMessages(prev => [...prev, newMessage, placeholder]);
        const placeholderId = placeholder.id;
        const userContent = currentInput;
        setCurrentInput('');
        
        try {
            const token = await getAccessTokenSilently({
                authorizationParams: { audience: import.meta.env.VITE_AUTH0_AUDIENCE },
            });
            
            await streamChat(userContent, token, (event) => {
                if (event.type === 'sources') {
                    setSources(event.value);
                } else if (event.type === 'token') {
                    setMessages(prev => prev.map(msg => 
                        msg.id === placeholderId
                            ? {...msg, content: msg.content + event.value, status: 'thinking' }
                            : msg
                    ));
                }
            });
            
            setMessages(prev => prev.map(msg => 
                msg.id === placeholderId
                    ? {...msg, status: 'complete' }
                    : msg
            ));
        } catch {
            setMessages(prev => prev.map(msg =>
                msg.id === placeholderId
                    ? {...msg, content: 'Something went wrong. Please try again.', status: 'complete'}
                    : msg
            ));
        }
    };
    
    return {
        messages,
        sources,
        currentInput,
        setCurrentInput,
        isThinking,
        endRef,
        handleSubmit
    }
}