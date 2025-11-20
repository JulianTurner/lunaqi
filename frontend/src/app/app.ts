import { Component, signal, OnInit, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ApiService } from '../test.service';
import { DatePipe, JsonPipe } from '@angular/common';
import { CalendarPreviousViewDirective, CalendarTodayDirective, CalendarNextViewDirective, CalendarMonthViewComponent, CalendarWeekViewComponent, CalendarDayViewComponent, CalendarDatePipe, DateAdapter, provideCalendar, CalendarEvent, CalendarView } from 'angular-calendar';
import { adapterFactory } from 'angular-calendar/date-adapters/date-fns';
import { endOfDay, isSameDay, isSameMonth, isWithinInterval, startOfDay } from 'date-fns';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, DatePipe, JsonPipe, CalendarPreviousViewDirective, CalendarTodayDirective, CalendarNextViewDirective, CalendarMonthViewComponent, CalendarWeekViewComponent, CalendarDayViewComponent, CalendarDatePipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  providers: [
    provideCalendar({
      provide: DateAdapter,
      useFactory: adapterFactory,
    }),
  ],
})
export class App implements OnInit {
  protected readonly title = signal('lunaqi');
  readonly message = signal('Hello from App Component');
  readonly phases = signal<any[]>([]);

  view: CalendarView = CalendarView.Month;

  readonly CalendarView = CalendarView;
  viewDate = new Date();

    // Colors for events
  private readonly colors: Record<'enabled' | 'disabled', { primary: string; secondary: string }> = {
    enabled: { primary: '#16a34a', secondary: '#bbf7d0' },   // green
    disabled: { primary: '#9ca3af', secondary: '#e5e7eb' },  // gray
  };

    activeDayIsOpen = signal<boolean>(true);
  selectedDate = signal<Date>(this.viewDate);

  // Derive events from phases
  readonly events = computed<CalendarEvent[]>(() =>
    this.phases().map((p) => ({
      start: new Date(p.startDate),
      end: new Date(p.endDate),
      title: `${p.phaseName}${p.isEnabled ? '' : ' (disabled)'}`,
      allDay: true,
      color: p.isEnabled ? this.colors.enabled : this.colors.disabled,
      meta: p
    }))
  );


  // Reaktion auf Tagesklick in der Month-View
  dayClicked(day: { date: Date; events: CalendarEvent[] }): void {
    if (!isSameMonth(day.date, this.viewDate)) return;

    const clickedSameAsSelected = isSameDay(this.selectedDate(), day.date);
    // Toggle-Logik analog zum Demo
    if ((clickedSameAsSelected && this.activeDayIsOpen()) || day.events.length === 0) {
      this.activeDayIsOpen.set(false);
    } else {
      this.activeDayIsOpen.set(true);
    }

    this.selectedDate.set(day.date);
    this.viewDate = day.date;
  }


  private eventHitsDay = (event: CalendarEvent, day: Date): boolean => {
    const start = startOfDay(event.start);
    const end = endOfDay(event.end ?? event.start);
    return isWithinInterval(day, { start, end });
  };

  // Events des selektierten Tages
  readonly selectedDayEvents = computed<CalendarEvent[]>(() => {
    const d = this.selectedDate();
    return this.events()
      .filter(e => this.eventHitsDay(e, d))
      .sort((a, b) => a.start.getTime() - b.start.getTime());
  });

  setView(view: CalendarView) {
    this.view = view;
  }

    // referenced by (viewDateChange) on the nav buttons
  closeOpenMonthViewDay(): void {
    // no-op for now; implement toggle logic if you need it
  }

  constructor(private api: ApiService) {

  }

  ngOnInit() {
    // add error case handling
    this.api.getHello().subscribe({
      next: (response) => this.message.set(response.message),
      error: (err) => console.error('GET /api/hello failed', err)
    });
    this.api.getPhases().subscribe({
      next: (response) => this.phases.set(response),
      error: (err) => console.error('GET phases failed', err)
    });
  }
}
