export type Message = {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  status: 'thinking' | 'complete';
  timestamp: Date;
};