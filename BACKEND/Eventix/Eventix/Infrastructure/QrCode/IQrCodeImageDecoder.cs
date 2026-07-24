namespace Eventix.Infrastructure.QrCode;

public interface IQrCodeImageDecoder
{
    Task<string?> DecodeAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
}