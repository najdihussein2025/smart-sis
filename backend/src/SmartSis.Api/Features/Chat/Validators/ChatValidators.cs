using FluentValidation;
using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Services;

namespace SmartSis.Api.Features.Chat.Validators;

internal sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.Message).Must(m => !string.IsNullOrWhiteSpace(m)).WithMessage("Type a message.")
            .MaximumLength(4000).WithMessage("Keep the message under 4000 characters.");
        RuleFor(x => x.ConversationId).GreaterThan(0).When(x => x.ConversationId is not null);
        RuleFor(x => x.CourseId).NotNull().WithMessage("Choose a course to get tutoring for.")
            .When(x => x.ConversationId is null && x.Mode == ChatMode.Tutor);
    }
}

internal sealed class RenameConversationRequestValidator : AbstractValidator<RenameConversationRequest>
{
    public RenameConversationRequestValidator()
    {
        RuleFor(x => x.Title).Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Enter a title.")
            .MaximumLength(200).WithMessage("Keep the title under 200 characters.");
    }
}

internal sealed class UploadKnowledgeRequestValidator : AbstractValidator<UploadKnowledgeRequest>
{
    private static readonly string[] Extensions = [".txt", ".md"];

    public UploadKnowledgeRequestValidator(IOptions<KnowledgeBaseOptions> options)
    {
        var max = options.Value.MaxFileBytes;
        RuleFor(x => x.Title).Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Enter a title.")
            .MaximumLength(200).WithMessage("Keep the title under 200 characters.");
        RuleFor(x => x.File).NotNull().WithMessage("Choose a file.");
        RuleFor(x => x.File!)
            .Must(f => Extensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Upload a .txt or .md file.")
            .Must(f => f.Length > 0).WithMessage("The file is empty.")
            .Must(f => f.Length <= max).WithMessage($"The file must be {max / 1_000_000.0:0.#} MB or smaller.")
            .When(x => x.File is not null);
    }
}
