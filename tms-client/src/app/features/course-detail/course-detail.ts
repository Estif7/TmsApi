import { Component, input, effect } from "@angular/core";
import { RouterLink } from '@angular/router';

@Component({
  selector: "app-course-detail",
  standalone: true,
  templateUrl: "./course-detail.html",
  imports: [RouterLink]
})

export class CourseDetail {
  id = input.required<string>();
  constructor() {
    effect(() => {
      console.log(`Loading course detail for ID: ${this.id()}`);
    });
  }
}