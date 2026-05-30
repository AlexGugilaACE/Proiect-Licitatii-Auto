using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

[Authorize]
public class TransactionsController(
    ITransactionService transactionService,
    ApplicationDbContext db,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transactions = await transactionService.GetUserTransactionsAsync(userId, cancellationToken);
        return View(transactions);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transaction = await transactionService.GetByIdAsync(id, cancellationToken);
        if (transaction is null || (transaction.SellerId != userId && transaction.BuyerId != userId))
        {
            return NotFound();
        }

        ViewBag.Messages = await BuildTransactionMessagesAsync(id, cancellationToken);
        return View(transaction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await transactionService.ConfirmAsync(id, userId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await transactionService.CancelAsync(id, userId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPaymentProof(int id, IFormFile paymentProof, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transaction = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transaction is null || transaction.BuyerId != userId)
        {
            return NotFound();
        }

        if (paymentProof is null || paymentProof.Length == 0)
        {
            TempData["ErrorMessage"] = "Selecteaza un fisier pentru dovada platii.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var extension = Path.GetExtension(paymentProof.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            TempData["ErrorMessage"] = "Dovada platii trebuie sa fie imagine sau PDF.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (paymentProof.Length > 5 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "Fisierul poate avea maximum 5 MB.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var proofFolder = Path.Combine(environment.WebRootPath, "uploads", "payment-proofs");
        Directory.CreateDirectory(proofFolder);
        DeletePaymentProofFile(transaction.PaymentProofPath);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(proofFolder, fileName);
        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await paymentProof.CopyToAsync(stream, cancellationToken);
        }

        transaction.PaymentProofPath = $"/uploads/payment-proofs/{fileName}";
        transaction.PaymentProofUploadedAt = DateTime.UtcNow;
        transaction.PaymentProofStatus = PaymentProofStatus.PendingReview;
        db.Notifications.Add(new Notification
        {
            UserId = transaction.SellerId,
            Title = "Dovada de plata incarcata",
            Message = $"Cumparatorul a incarcat dovada de plata pentru tranzactia #{transaction.Id}.",
            Type = NotificationType.PaymentProofUploaded
        });
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Dovada platii a fost incarcata.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewPaymentProof(int id, PaymentProofStatus status, CancellationToken cancellationToken)
    {
        if (status is not PaymentProofStatus.Accepted and not PaymentProofStatus.Rejected)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transaction = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transaction is null || transaction.SellerId != userId || string.IsNullOrWhiteSpace(transaction.PaymentProofPath))
        {
            return NotFound();
        }

        transaction.PaymentProofStatus = status;
        db.Notifications.Add(new Notification
        {
            UserId = transaction.BuyerId,
            Title = status == PaymentProofStatus.Accepted ? "Dovada de plata acceptata" : "Dovada de plata respinsa",
            Message = status == PaymentProofStatus.Accepted
                ? $"Dovada de plata pentru tranzactia #{transaction.Id} a fost acceptata."
                : $"Dovada de plata pentru tranzactia #{transaction.Id} a fost respinsa. Te rugam sa verifici si sa incarci din nou daca este necesar.",
            Type = status == PaymentProofStatus.Accepted ? NotificationType.PaymentProofAccepted : NotificationType.PaymentProofRejected
        });
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = status == PaymentProofStatus.Accepted
            ? "Dovada platii a fost acceptata."
            : "Dovada platii a fost respinsa.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int id, CreateTransactionMessageViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var transaction = await db.Transactions
            .Include(x => x.Auction)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (transaction is null || (transaction.SellerId != userId && transaction.BuyerId != userId))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Mesajul trebuie completat si poate avea maximum 1200 de caractere.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var receiverId = transaction.SellerId == userId ? transaction.BuyerId : transaction.SellerId;
        db.TransactionMessages.Add(new TransactionMessage
        {
            TransactionId = id,
            SenderId = userId,
            Message = model.Message.Trim()
        });
        db.Notifications.Add(new Notification
        {
            UserId = receiverId,
            Title = "Mesaj nou in tranzactie",
            Message = $"Ai primit un mesaj nou pentru tranzactia #{transaction.Id}.",
            Type = NotificationType.TransactionMessage
        });

        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    private void DeletePaymentProofFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith("/uploads/payment-proofs/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var absolutePath = Path.Combine(environment.WebRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }

    private async Task<IReadOnlyList<TransactionMessageListItemViewModel>> BuildTransactionMessagesAsync(int transactionId, CancellationToken cancellationToken)
    {
        var messages = await db.TransactionMessages
            .AsNoTracking()
            .Where(x => x.TransactionId == transactionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var senderIds = messages.Select(x => x.SenderId).Distinct().ToList();
        var senders = await db.Users
            .AsNoTracking()
            .Where(x => senderIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return messages.Select(message =>
        {
            senders.TryGetValue(message.SenderId, out var sender);
            var senderName = sender is null ? "Utilizator" : $"{sender.FirstName} {sender.LastName}".Trim();

            return new TransactionMessageListItemViewModel
            {
                SenderId = message.SenderId,
                SenderName = string.IsNullOrWhiteSpace(senderName) ? sender?.Email ?? "Utilizator" : senderName,
                Message = message.Message,
                CreatedAt = message.CreatedAt
            };
        }).ToList();
    }
}
