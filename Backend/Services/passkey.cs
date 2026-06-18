using System.Buffers.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Backend.Models;


public class PasskeyService
{
    private readonly DBcontext _context;
    private readonly IFido2 _fido;


    public PasskeyService(
        DBcontext db,
        IFido2 fido)
    {
        _context = db;
        _fido = fido;
    }



    // ===========================
    // REGISTER START
    // ===========================

    public CredentialCreateOptions RegisterOptions(
        Guid userId)
    {

        var user = _context.Cur
            .First(x => x.Uuid == userId);


        return _fido.RequestNewCredential(
            new RequestNewCredentialParams
            {
                User = new Fido2User
                {
                    Id = user.Uuid.ToByteArray(),
                    Name = user.Email,
                    DisplayName = user.Email
                },

                ExcludeCredentials =
                    new List<PublicKeyCredentialDescriptor>(),

                AuthenticatorSelection =
                    new AuthenticatorSelection
                    {
                        UserVerification =
                            UserVerificationRequirement.Required,

                        ResidentKey =
                            ResidentKeyRequirement.Preferred
                    }
            });
    }



    // ===========================
    // REGISTER VERIFY
    // ===========================

    public async Task<bool> RegisterVerify(
        Guid userId,
        AuthenticatorAttestationRawResponse response,
        CredentialCreateOptions options)
    {

        // MakeNewCredentialAsync throws on failure in v4
        // result is RegisteredPublicKeyCredential directly
        var result =
            await _fido.MakeNewCredentialAsync(
                new MakeNewCredentialParams
                {
                    AttestationResponse = response,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback =
                        async (args, ct) =>
                        {
                            return !await _context.UserPasskeys
                                .AnyAsync(x =>
                                    x.CredentialId ==
                                    Convert.ToBase64String(
                                        args.CredentialId));
                        }
                });


        _context.UserPasskeys.Add(
            new Passkey
            {
                UserUuid = userId,

                CredentialId = Base64Url.EncodeToString(result.Id),

                PublicKey =
                    Convert.ToBase64String(
                        result.PublicKey),

                SignCount =
                    result.SignCount
            });


        await _context.SaveChangesAsync();


        return true;
    }



    // ===========================
    // LOGIN OPTIONS
    // ===========================

    public async Task<AssertionOptions> LoginOptions(
        string email)
    {

        var user = await _context.Cur
            .FirstAsync(x => x.Email == email);


        var credentials =
            await _context.UserPasskeys
            .Where(x => x.UserUuid == user.Uuid)
            .ToListAsync();


        var allowed =
            credentials
            .Select(x =>
                new PublicKeyCredentialDescriptor(
                    Convert.FromBase64String(
                        x.CredentialId)))
            .ToList();


        return _fido.GetAssertionOptions(
            new GetAssertionOptionsParams
            {
                AllowedCredentials = allowed,
                UserVerification = UserVerificationRequirement.Required
            });
    }



    // ===========================
    // LOGIN VERIFY
    // ===========================

    public async Task<bool> LoginVerify(
        string email,
        AuthenticatorAssertionRawResponse response,
        AssertionOptions options)
    {

        var credentialIdBase64 =
            Convert.ToBase64String(response.RawId);

        var credential =
            await _context.UserPasskeys
            .FirstOrDefaultAsync(x =>
                x.CredentialId == credentialIdBase64);


        if (credential == null)
            return false;


        // MakeAssertionAsync throws on failure in v4
        var result =
            await _fido.MakeAssertionAsync(
                new MakeAssertionParams
                {
                    AssertionResponse = response,
                    OriginalOptions = options,
                    StoredPublicKey =
                        Convert.FromBase64String(
                            credential.PublicKey),
                    StoredSignatureCounter =
                        (uint)credential.SignCount,
                    IsUserHandleOwnerOfCredentialIdCallback =
                        (args, ct) => Task.FromResult(true)
                });


        credential.SignCount = result.SignCount;

        await _context.SaveChangesAsync();


        return true;
    }
}