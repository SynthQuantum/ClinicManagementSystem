using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Services.Tests.Builders;

/// <summary>Fluent builder for <see cref="Patient"/> test fixtures.</summary>
public sealed class PatientBuilder
{
    private string _firstName = "Test";
    private string _lastName = "Patient";
    private DateTime _dateOfBirth = new DateTime(1990, 6, 15);
    private string? _email;
    private string? _phoneNumber;
    private Gender _gender = Gender.Male;
    private string? _notes;

    public static PatientBuilder Default() => new();

    public PatientBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public PatientBuilder WithDateOfBirth(DateTime dob)
    {
        _dateOfBirth = dob;
        return this;
    }

    public PatientBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public PatientBuilder WithPhone(string phone)
    {
        _phoneNumber = phone;
        return this;
    }

    public PatientBuilder WithGender(Gender gender)
    {
        _gender = gender;
        return this;
    }

    public PatientBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public Patient Build() => new()
    {
        FirstName = _firstName,
        LastName = _lastName,
        DateOfBirth = _dateOfBirth,
        Email = _email,
        PhoneNumber = _phoneNumber,
        Gender = _gender,
        Notes = _notes
    };
}
