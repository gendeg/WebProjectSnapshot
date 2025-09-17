using Fetch.Builder;
using Presentation;
using System.Text.Json;
using DB.WideColumns;
using Sessions;
using Authentication;

namespace Fetch.PostComment;


public class SubmitPostComment(JsonElement requestArg) : IFetchBuilder(requestArg)
{
    WideColumn db = new();

    public override void BuildResponseValues()
    {
        // Ensure request has all required fields
        if (!(requestDoc.TryGetProperty("postId", out JsonElement postIdEl)
            && requestDoc.TryGetProperty("nonceVal", out JsonElement nonceEl)
            && requestDoc.TryGetProperty("commentText", out JsonElement textEl)))
        {
            Return404();
            return;
        }

        // Ensure postId and nonce are valid values
        string nonce = nonceEl.GetString()!;
        if (!(UInt128.TryParse(postIdEl.GetString(), out UInt128 postId) && Pres.ValidateNonce(nonce)))
        {
            Return404();
            return;
        }

        // Ensure session has a nonce for this action
        CurrentSession session = (CurrentSession)ThisContext.Get().Items["currentSession"]!;
        if (!session.tokens.TryGetPrior("postCommentNonce", out string? validNonce))
        {
            Return404();
            return;
        }

        // Ensure nonce is the correct nonce
        if (nonce != validNonce)
        {
            Return404();
            return;
        }

        string commentText = Pres.MakeHTMLSafe(textEl.GetString()!);

        // TODO: set secondary id to top level ancestor comment, or 0 if no ancestors
        UInt128 commentId = db.Insert("post_comments", postId, 0, commentText);

        AddValue("commentId", commentId.ToString());
    }
}