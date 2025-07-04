﻿using Avancira.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Messaging.Dtos
{
    public class ChatDto
    {
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public string TutorId { get; set; }
        public string StudentId { get; set; }
        public string RecipientId { get; set; }
        public string Name { get; set; }
        // URL for the profile image of the chat participant
        public string ImageUrl { get; set; }
        public string LastMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
        public List<MessageDto> Messages { get; set; }
        public UserRole MyRole { get; set; }

        public ChatDto()
        {
            Name = string.Empty;
            ImageUrl = string.Empty;
            TutorId = string.Empty;
            StudentId = string.Empty;
            RecipientId = string.Empty;
            LastMessage = string.Empty;
            Details = string.Empty;
            Messages = new List<MessageDto>();
        }
    }
}
