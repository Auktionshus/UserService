using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
    }
