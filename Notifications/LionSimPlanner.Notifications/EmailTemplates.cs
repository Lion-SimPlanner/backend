namespace LionSimPlanner.Notifications;

/// <summary>
/// HTML email template for session scheduled notifications.
/// Sent to assigned pilots when a session transitions DRAFT → SCHEDULED.
///
/// Note: Uses $$ raw string literal so CSS braces {} can coexist with
/// C# interpolation {{expr}} without conflict.
/// </summary>
internal static class SessionScheduledTemplate
{
    public static string Build(
        Guid sessionId,
        DateTime startTime,
        DateTime endTime,
        string simulatorName,
        string syllabusId)
    {
        // $$ prefix: C# expressions use {{expr}}, CSS braces stay as { }
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>Session Scheduled — Lion SimPlanner</title>
          <style>
            body  { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
                    background: #f4f6f9; margin: 0; padding: 0; color: #1a1a2e; }
            .wrap { max-width: 600px; margin: 40px auto; background: #ffffff;
                    border-radius: 12px; overflow: hidden;
                    box-shadow: 0 4px 24px rgba(0,0,0,0.08); }
            .hdr  { background: linear-gradient(135deg, #1a1a2e 0%, #16213e 60%, #0f3460 100%);
                    padding: 32px 40px; }
            .hdr h1 { color: #e2b96f; margin: 0; font-size: 22px; letter-spacing: 0.5px; }
            .hdr p  { color: #a8b2d8; margin: 8px 0 0; font-size: 14px; }
            .body { padding: 32px 40px; }
            .badge { display: inline-block; background: #22c55e; color: #fff;
                     border-radius: 20px; padding: 4px 14px; font-size: 12px;
                     font-weight: 600; letter-spacing: 0.5px; margin-bottom: 24px; }
            .row  { display: flex; justify-content: space-between;
                    border-bottom: 1px solid #f0f0f0; padding: 12px 0; }
            .row:last-child { border-bottom: none; }
            .lbl  { color: #6b7280; font-size: 13px; font-weight: 500; }
            .val  { color: #1a1a2e; font-size: 13px; font-weight: 600; text-align: right; }
            .ftr  { background: #f8fafc; padding: 20px 40px; text-align: center;
                    color: #9ca3af; font-size: 12px; border-top: 1px solid #e5e7eb; }
          </style>
        </head>
        <body>
          <div class="wrap">
            <div class="hdr">
              <h1>🦁 Lion SimPlanner</h1>
              <p>Simulator Training Schedule Notification</p>
            </div>
            <div class="body">
              <div class="badge">✓ SESSION CONFIRMED</div>
              <p style="color:#374151;font-size:15px;margin-bottom:24px;">
                Your simulator training session has been confirmed and published.
                Please review the details below and ensure you are available at the scheduled time.
              </p>
              <div class="row">
                <span class="lbl">Session ID</span>
                <span class="val" style="font-size:11px;color:#9ca3af;">{{sessionId}}</span>
              </div>
              <div class="row">
                <span class="lbl">Simulator Bay</span>
                <span class="val">{{simulatorName}}</span>
              </div>
              <div class="row">
                <span class="lbl">Date</span>
                <span class="val">{{startTime:dddd, MMMM dd, yyyy}}</span>
              </div>
              <div class="row">
                <span class="lbl">Start Time (UTC)</span>
                <span class="val">{{startTime:HH:mm}} UTC</span>
              </div>
              <div class="row">
                <span class="lbl">End Time (UTC)</span>
                <span class="val">{{endTime:HH:mm}} UTC</span>
              </div>
              <div class="row">
                <span class="lbl">Duration</span>
                <span class="val">{{(endTime - startTime).TotalHours:F1}} hours</span>
              </div>
              <div class="row">
                <span class="lbl">Syllabus</span>
                <span class="val">{{syllabusId}}</span>
              </div>
              <p style="margin-top:24px;color:#6b7280;font-size:13px;">
                This is an automated notification from Lion SimPlanner.
                For scheduling changes, contact your Training Administrator.
              </p>
            </div>
            <div class="ftr">
              Lion SimPlanner &middot; Level D Full Flight Simulator Operations &middot; {{DateTime.UtcNow.Year}}
            </div>
          </div>
        </body>
        </html>
        """;
    }
}

/// <summary>
/// HTML email template for session reschedule notifications.
/// Sent to assigned pilots when a session's start/end time changes.
/// </summary>
internal static class SessionRescheduledTemplate
{
    public static string Build(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        DateTime newStartTime,
        DateTime newEndTime,
        string simulatorName,
        string syllabusId)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>Session Rescheduled — Lion SimPlanner</title>
          <style>
            body  { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
                    background: #f4f6f9; margin: 0; padding: 0; color: #1a1a2e; }
            .wrap { max-width: 600px; margin: 40px auto; background: #ffffff;
                    border-radius: 12px; overflow: hidden;
                    box-shadow: 0 4px 24px rgba(0,0,0,0.08); }
            .hdr  { background: linear-gradient(135deg, #1a1a2e 0%, #3b0764 100%);
                    padding: 32px 40px; }
            .hdr h1 { color: #e2b96f; margin: 0; font-size: 22px; letter-spacing: 0.5px; }
            .hdr p  { color: #a8b2d8; margin: 8px 0 0; font-size: 14px; }
            .body { padding: 32px 40px; }
            .badge { display: inline-block; background: #f59e0b; color: #fff;
                     border-radius: 20px; padding: 4px 14px; font-size: 12px;
                     font-weight: 600; letter-spacing: 0.5px; margin-bottom: 24px; }
            .row  { display: flex; justify-content: space-between;
                    border-bottom: 1px solid #f0f0f0; padding: 12px 0; }
            .row:last-child { border-bottom: none; }
            .lbl  { color: #6b7280; font-size: 13px; font-weight: 500; }
            .val  { color: #1a1a2e; font-size: 13px; font-weight: 600; text-align: right; }
            .old  { color: #9ca3af; text-decoration: line-through; font-weight: 500; }
            .new  { color: #16a34a; font-weight: 700; }
            .ftr  { background: #f8fafc; padding: 20px 40px; text-align: center;
                    color: #9ca3af; font-size: 12px; border-top: 1px solid #e5e7eb; }
          </style>
        </head>
        <body>
          <div class="wrap">
            <div class="hdr">
              <h1>🦁 Lion SimPlanner</h1>
              <p>Simulator Training Schedule Notification</p>
            </div>
            <div class="body">
              <div class="badge">✏ SESSION RESCHEDULED</div>
              <p style="color:#374151;font-size:15px;margin-bottom:24px;">
                Your simulator training session has been rescheduled.
                Please review the updated details below.
              </p>
              <div class="row">
                <span class="lbl">Session ID</span>
                <span class="val" style="font-size:11px;color:#9ca3af;">{{sessionId}}</span>
              </div>
              <div class="row">
                <span class="lbl">Simulator Bay</span>
                <span class="val">{{simulatorName}}</span>
              </div>
              <div class="row">
                <span class="lbl">Syllabus</span>
                <span class="val">{{syllabusId}}</span>
              </div>
              <div class="row">
                <span class="lbl">Previously Scheduled</span>
                <span class="val old">{{originalStartTime:dddd, MMMM dd, yyyy HH:mm}} — {{originalEndTime:HH:mm}} UTC</span>
              </div>
              <div class="row">
                <span class="lbl">New Start (UTC)</span>
                <span class="val new">{{newStartTime:dddd, MMMM dd, yyyy HH:mm}} UTC</span>
              </div>
              <div class="row">
                <span class="lbl">New End (UTC)</span>
                <span class="val new">{{newEndTime:HH:mm}} UTC</span>
              </div>
              <div class="row">
                <span class="lbl">New Duration</span>
                <span class="val new">{{(newEndTime - newStartTime).TotalHours:F1}} hours</span>
              </div>
              <p style="margin-top:24px;color:#6b7280;font-size:13px;">
                This is an automated notification from Lion SimPlanner.
                For scheduling changes, contact your Training Administrator.
              </p>
            </div>
            <div class="ftr">
              Lion SimPlanner &middot; Level D Full Flight Simulator Operations &middot; {{DateTime.UtcNow.Year}}
            </div>
          </div>
        </body>
        </html>
        """;
    }
}

/// <summary>
/// HTML email template for session cancellation alerts.
/// Includes the specific cancellation reason (AOG fault description or Admin reason).
/// </summary>
internal static class SessionCancelledTemplate
{
    public static string Build(
        Guid sessionId,
        DateTime originalStartTime,
        DateTime originalEndTime,
        string cancellationReason)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>Session Cancelled — Lion SimPlanner</title>
          <style>
            body  { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
                    background: #f4f6f9; margin: 0; padding: 0; color: #1a1a2e; }
            .wrap { max-width: 600px; margin: 40px auto; background: #ffffff;
                    border-radius: 12px; overflow: hidden;
                    box-shadow: 0 4px 24px rgba(0,0,0,0.08); }
            .hdr  { background: linear-gradient(135deg, #1a1a2e 0%, #7f1d1d 100%);
                    padding: 32px 40px; }
            .hdr h1 { color: #fca5a5; margin: 0; font-size: 22px; letter-spacing: 0.5px; }
            .hdr p  { color: #fecaca; margin: 8px 0 0; font-size: 14px; }
            .body { padding: 32px 40px; }
            .badge { display: inline-block; background: #ef4444; color: #fff;
                     border-radius: 20px; padding: 4px 14px; font-size: 12px;
                     font-weight: 600; letter-spacing: 0.5px; margin-bottom: 24px; }
            .row  { display: flex; justify-content: space-between;
                    border-bottom: 1px solid #f0f0f0; padding: 12px 0; }
            .row:last-child { border-bottom: none; }
            .lbl  { color: #6b7280; font-size: 13px; font-weight: 500; }
            .val  { color: #1a1a2e; font-size: 13px; font-weight: 600; text-align: right; }
            .reason { background: #fef2f2; border-left: 4px solid #ef4444;
                      border-radius: 4px; padding: 16px; margin: 20px 0;
                      color: #7f1d1d; font-size: 13px; line-height: 1.6; }
            .ftr  { background: #f8fafc; padding: 20px 40px; text-align: center;
                    color: #9ca3af; font-size: 12px; border-top: 1px solid #e5e7eb; }
          </style>
        </head>
        <body>
          <div class="wrap">
            <div class="hdr">
              <h1>🦁 Lion SimPlanner</h1>
              <p>Session Cancellation Alert</p>
            </div>
            <div class="body">
              <div class="badge">⚠ SESSION CANCELLED</div>
              <p style="color:#374151;font-size:15px;margin-bottom:24px;">
                Your simulator training session has been cancelled.
                Please contact your Training Administrator to reschedule.
              </p>
              <div class="row">
                <span class="lbl">Session ID</span>
                <span class="val" style="font-size:11px;color:#9ca3af;">{{sessionId}}</span>
              </div>
              <div class="row">
                <span class="lbl">Originally Scheduled</span>
                <span class="val">{{originalStartTime:dddd, MMMM dd, yyyy}}</span>
              </div>
              <div class="row">
                <span class="lbl">Original Start (UTC)</span>
                <span class="val">{{originalStartTime:HH:mm}} UTC</span>
              </div>
              <div class="row">
                <span class="lbl">Original End (UTC)</span>
                <span class="val">{{originalEndTime:HH:mm}} UTC</span>
              </div>
              <div class="reason">
                <strong>Cancellation Reason:</strong><br/>
                {{cancellationReason}}
              </div>
              <p style="color:#6b7280;font-size:13px;">
                This cancellation was processed automatically by Lion SimPlanner.
                Contact your Training Administrator to arrange a replacement session.
              </p>
            </div>
            <div class="ftr">
              Lion SimPlanner &middot; Level D Full Flight Simulator Operations &middot; {{DateTime.UtcNow.Year}}
            </div>
          </div>
        </body>
        </html>
        """;
    }
}
