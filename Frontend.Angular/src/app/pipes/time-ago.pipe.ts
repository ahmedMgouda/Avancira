import { Pipe, PipeTransform } from '@angular/core';
import { formatDistanceToNowStrict, parseISO } from 'date-fns';

@Pipe({
  name: 'timeAgo'
})
export class TimeAgoPipe implements PipeTransform {
  transform(value: string | Date): string {
    if (!value) return 'time ago';

    const date = typeof value === 'string' ? parseISO(value) : value;
    
    return formatDistanceToNowStrict(date, { addSuffix: true }); 
  }
}
