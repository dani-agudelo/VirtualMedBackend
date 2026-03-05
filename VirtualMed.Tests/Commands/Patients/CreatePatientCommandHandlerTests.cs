using Moq;
using VirtualMed.Application.Commands.Patients;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Tests.Commands.Patients;

public class CreatePatientCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IPatientRepository> _patientRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRoleRepository> _roleRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

    private CreatePatientCommandHandler CreateHandler() =>
        new CreatePatientCommandHandler(
            _contextMock.Object,
            _patientRepoMock.Object,
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _passwordHasherMock.Object);

    [Fact]
    public async Task Handle_SavesUserAndPatientAtomically_InSingleSaveChangesCall()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Name = "Patient" };
        _roleRepoMock.Setup(r => r.GetByNameAsync("Patient")).ReturnsAsync(role);
        _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed-password");

        var saveCallCount = 0;
        var userAddedBeforeSave = false;
        var patientAddedBeforeSave = false;

        _userRepoMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(_ => userAddedBeforeSave = true)
            .Returns(Task.CompletedTask);

        _patientRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .Callback<Patient>(_ => patientAddedBeforeSave = true)
            .Returns(Task.CompletedTask);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => saveCallCount++)
            .ReturnsAsync(2);

        var command = new CreatePatientCommand(
            FullName: "Jane Doe",
            Email: "jane@example.com",
            Password: "Secret123!",
            ConfirmPassword: "Secret123!",
            IdentificationType: IdentificationType.CC,
            Document: "123456789",
            DateOfBirth: new DateOnly(1990, 1, 1),
            Gender: "Female",
            PhoneNumber: "3001234567",
            AcceptPrivacy: true,
            AuthorizeData: true);

        var handler = CreateHandler();

        // Act
        var patientId = await handler.Handle(command, CancellationToken.None);

        // Assert: both entities were tracked before the single save
        Assert.True(userAddedBeforeSave, "User should be added before SaveChangesAsync.");
        Assert.True(patientAddedBeforeSave, "Patient should be added before SaveChangesAsync.");
        Assert.Equal(1, saveCallCount);
        Assert.NotEqual(Guid.Empty, patientId);

        // Verify repositories did not trigger extra saves on their own
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _patientRepoMock.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsInvalidOperationException_WhenPatientRoleNotFound()
    {
        // Arrange
        _roleRepoMock.Setup(r => r.GetByNameAsync("Patient")).ReturnsAsync((Role?)null);

        var command = new CreatePatientCommand(
            FullName: "Jane Doe",
            Email: "jane@example.com",
            Password: "Secret123!",
            ConfirmPassword: "Secret123!",
            IdentificationType: IdentificationType.CC,
            Document: "123456789",
            DateOfBirth: new DateOnly(1990, 1, 1),
            Gender: "Female",
            PhoneNumber: "3001234567",
            AcceptPrivacy: true,
            AuthorizeData: true);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _patientRepoMock.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
