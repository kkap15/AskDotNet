import type { ChunkReference } from "../types/ChunkReference";

export type SseEvent = 
    | { type: 'token'; value: string }
    | { type: 'sources'; value: ChunkReference[] }
    
export function parseSseLines(lines: string[], isSource: boolean): {
    events: SseEvent[],
    isSource: boolean;
} {
    const events: SseEvent[] = [];

    for (const line of lines) {
        if (line === 'event: sources') {
            isSource = true;
            continue;
        }
        if (line.startsWith('data: ')) {
            const data = line.slice(6);
            if (isSource) {
                events.push({ type: 'sources', value: JSON.parse(data) as ChunkReference[] });
                isSource = false;
            } else {
                events.push({ type: 'token', value: data });
            }
        }
    }

    return { events, isSource };
}