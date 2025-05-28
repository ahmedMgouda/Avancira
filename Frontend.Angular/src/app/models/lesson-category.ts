export class LessonCategory {
  id: number;
  name: string;
  courses: number;
  image: string;

  constructor(id: number, name: string, courses: number, image: string) {
    this.id = id;
    this.name = name;
    this.courses = courses;
    this.image = image;
  }
}
