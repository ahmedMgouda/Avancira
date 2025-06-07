export class LessonCategory {
  id: string; // Changed from number to string to match API Guid
  name: string;
  courses: number;
  image: string;

  constructor(id: string, name: string, courses: number, image: string) {
    this.id = id;
    this.name = name;
    this.courses = courses;
    this.image = image;
  }
}
