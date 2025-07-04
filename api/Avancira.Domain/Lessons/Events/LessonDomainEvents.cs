﻿using Avancira.Domain.Common.Events;
using Avancira.Domain.Lessons;

namespace Avancira.Domain.Lessons.Events
{
    public record LessonCreatedEvent(Lesson Lesson) : DomainEvent;
    public record LessonStatusChangedEvent(Lesson Lesson, LessonStatus OldStatus, LessonStatus NewStatus) : DomainEvent;
}
