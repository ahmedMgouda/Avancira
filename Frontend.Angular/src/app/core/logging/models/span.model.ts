export interface Span {
  spanId: string;
  name: string;
  startTime: Date;
  endTime?: Date;
  status: 'active' | 'ended' | 'error';
}

