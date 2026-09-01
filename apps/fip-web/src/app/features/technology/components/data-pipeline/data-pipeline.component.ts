import { DecimalPipe } from '@angular/common';
import { Component } from '@angular/core';
@Component({ selector:'app-data-pipeline', imports: [DecimalPipe], templateUrl:'./data-pipeline.component.html', styleUrl:'./data-pipeline.component.scss' })
export class DataPipelineComponent { readonly steps=['RAW FLIGHT DATA','IMPORT','VALIDATION','NORMALIZATION','FLIGHT RECONSTRUCTION','EVENT DETECTION','ANALYTICS','VISUALIZATION & INTELLIGENCE']; }
