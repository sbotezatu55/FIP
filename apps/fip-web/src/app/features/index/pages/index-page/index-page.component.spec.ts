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
    const exploreLink = element.querySelector('a.primary-button') as HTMLAnchorElement;

    expect(element.textContent).toContain('Flight Intelligence Platform');
    expect(element.textContent).toContain('Transforming aircraft trajectory data into meaningful flight intelligence.');
    expect(element.textContent).toContain('Import, reconstruct, visualize, and analyze flight telemetry and flight events.');
    expect(exploreLink.textContent?.trim()).toBe('Explore Flights');
    expect(exploreLink.getAttribute('href')).toBe('/flights');
  });
});
