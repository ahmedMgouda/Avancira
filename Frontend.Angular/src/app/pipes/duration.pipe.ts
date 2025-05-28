import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'duration'
})
export class DurationPipe implements PipeTransform {
  transform(value: string | null): string {
    if (!value) return '0 minutes';

    const parts = value.split(':'); // Split "hh:mm:ss"
    if (parts.length !== 3) return value; // Invalid format, return as is

    const hours = parseInt(parts[0], 10);
    const minutes = parseInt(parts[1], 10);

    let result = '';
    if (hours > 0) result += `${hours} hour${hours > 1 ? 's' : ''} `;
    if (minutes > 0) result += `${minutes} minute${minutes > 1 ? 's' : ''}`;

    return result.trim() || '0 minutes';
  }
}
