import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { IndexPageComponent } from './index-page.component';

describe('IndexPageComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IndexPageComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('renders the platform introduction and Explore Flights link', () => {
    const fixture = TestBed.createComponent(IndexPageComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const exploreLink = element.querySelector('a.fip-button--primary') as HTMLAnchorElement;

    expect(element.textContent).toContain('FlightIntelligencePlatform');
    expect(element.textContent).toContain('From raw telemetry toflight intelligence.');
    expect(element.textContent).toContain('Flight processing pipeline');
    expect(exploreLink.textContent).toContain('Explore a Flight');
    expect(exploreLink.getAttribute('href')).toBe('/flights');
  });
});
