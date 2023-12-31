﻿namespace Domain
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Connected { get; set; }


        private static int currentId = 0;

        public User()
        {
            currentId++;
            Id = currentId;
        }

        public override bool Equals(object? obj)
        {
            if (obj is User otherUser)
            {
                return this.Username == otherUser.Username && this.Password == otherUser.Password;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (this.Username?.GetHashCode() ?? 0);
                hash = hash * 23 + (this.Password?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
