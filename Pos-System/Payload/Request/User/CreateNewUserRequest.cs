﻿namespace Pos_System.API.Payload.Request.User
{
    public class CreateNewUserRequest
    {
        public string PhoneNunmer { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
    }
}