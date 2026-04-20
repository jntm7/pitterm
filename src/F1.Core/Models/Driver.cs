namespace F1.Core.Models;

public sealed record Driver(
    int DriverNumber,
    string FullName,
    string FirstName,
    string LastName,
    string NameAcronym,
    string TeamName,
    string TeamColour,
    string Nationality,
    DateOnly? DateOfBirth,
    string ProfileUrl);
