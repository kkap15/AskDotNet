import { parseSseLines, type SseEvent } from "../utils/sseParser";

export async function streamChat(
    question: string,
    token: string,
    onEvent: (event: SseEvent) => void
): Promise<void> {
    const response = await fetch(`${import.meta.env.VITE_API_URL}/api/chat`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ question })
    });
    
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    if (!response.body) throw new Error('No response body');
    
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
        
        const { events, isSource: nextIsSource } = parseSseLines(lines, isSource);
        isSource = nextIsSource;
        
        for (const event of events) {
            onEvent(event);
        }
    }
}