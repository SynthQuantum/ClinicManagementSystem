using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;

namespace ClinicManagementSystem.Services.Tests.Builders;

/// <summary>Fluent builder for <see cref="StaffMember"/> test fixtures.</summary>
public sealed class StaffMemberBuilder
{
    private string _firstName = "Dr.";
    private string _lastName = "Staff";
    private string _email = $"staff.{Guid.NewGuid():N}@test.local";
    private UserRole _role = UserRole.Doctor;
    private string? _specialty;
    private string? _phoneNumber;
    private bool _isAvailable = true;

    public static StaffMemberBuilder Default() => new();

    public StaffMemberBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public StaffMemberBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public StaffMemberBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public StaffMemberBuilder WithSpecialty(string specialty)
    {
        _specialty = specialty;
        return this;
    }

    public StaffMemberBuilder WithPhone(string phone)
    {
        _phoneNumber = phone;
        return this;
    }

    public StaffMemberBuilder Unavailable()
    {
        _isAvailable = false;
        return this;
    }

    public StaffMember Build() => new()
    {
        FirstName = _firstName,
        LastName = _lastName,
        Email = _email,
        Role = _role,
        Specialty = _specialty,
        PhoneNumber = _phoneNumber,
        IsAvailable = _isAvailable
    };
}
