import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AppComponent } from './app.component';
import { routes } from './app.routes';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter(routes)]
    }).compileComponents();
  });

  it('renders the application shell navigation', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('.app-nav a')) as HTMLAnchorElement[];

    expect(links.map((link) => link.textContent?.trim())).toEqual(['Flights', 'About', 'Features', 'Technology']);
  });

  it('configures the requested routes', () => {
    const router = TestBed.inject(Router);

    expect(router.config.map((route) => route.path)).toEqual(['', 'flights', 'flights/import', 'flights/:id']);
  });
});
