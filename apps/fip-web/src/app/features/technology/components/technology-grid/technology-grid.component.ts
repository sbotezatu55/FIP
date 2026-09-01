import { Component } from '@angular/core';

@Component({ selector: 'app-technology-grid', templateUrl: './technology-grid.component.html', styleUrl: './technology-grid.component.scss' })
export class TechnologyGridComponent {
  readonly items = [
    { symbol: '</>', name: '.NET / C#', description: 'Backend platform and primary implementation language for aviation domain logic, flight processing, APIs, persistence, and analytical services.' },
    { symbol: 'API', name: 'ASP.NET Core Web API', description: 'Provides the REST API boundary between the flight intelligence engine and applications consuming flight records, telemetry, summaries, and detected events.' },
    { symbol: 'TS', name: 'Angular / TypeScript', description: 'Provides the interactive web application used to explore reconstructed flights, trajectories, charts, events, and analytical results.' },
    { symbol: 'SQL', name: 'SQL Server', description: 'Provides persistent storage for normalized flights, telemetry, detected events, import information, and analytical results.' },
    { symbol: 'EF', name: 'Entity Framework Core', description: 'Provides the persistence and data-access layer between the FIP domain/application architecture and SQL Server.' }
  ];
}
