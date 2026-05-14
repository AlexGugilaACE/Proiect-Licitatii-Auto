using AutoAuction.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

[Authorize]
public class TransactionsController(ITransactionService transactionService) : Controller
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
}
