import {
  ChangeDetectionStrategy, Component, input, output,
} from '@angular/core';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventClickArg, EventInput } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import listPlugin from '@fullcalendar/list';
import interactionPlugin from '@fullcalendar/interaction';
import frLocale from '@fullcalendar/core/locales/fr';

export interface CalendarEventItem {
  id: string;
  title: string;
  start: string;
  end?: string;
  color?: string;
  extendedProps?: Record<string, unknown>;
}

@Component({
  selector: 'app-calendar-view',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FullCalendarModule],
  template: `
    <full-calendar [options]="calendarOptions()" />
  `,
})
export class CalendarViewComponent {
  readonly events = input.required<CalendarEventItem[]>();
  readonly locale = input<string>('fr');
  readonly eventClick = output<string>();

  calendarOptions(): CalendarOptions {
    return {
      plugins: [dayGridPlugin, timeGridPlugin, listPlugin, interactionPlugin],
      initialView: 'dayGridMonth',
      locale: this.locale() === 'fr' ? frLocale : undefined,
      headerToolbar: {
        left: 'prev,next today',
        center: 'title',
        right: 'dayGridMonth,timeGridWeek,listMonth',
      },
      events: this.events() as EventInput[],
      eventClick: (info: EventClickArg) => {
        this.eventClick.emit(info.event.id);
      },
      height: 'auto',
      eventDisplay: 'block',
    };
  }
}
