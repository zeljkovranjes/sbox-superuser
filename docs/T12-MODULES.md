# T12: Module lifecycle

Status: **In progress.** Implements dependency and API-version checks, staged registration, owner-only enable/disable, bounded cancellation and listener cleanup. Required security modules stay enabled. No stored data is deleted when a module stops.

The manager owns module registrations. Commands publish only after startup succeeds. Modules must register every listener's disposable subscription with their registration scope and honor cancellation; arbitrary trusted C# cannot be forcibly interrupted. A timed-out module remains quarantined until its work and cleanup finish, preventing overlapping reloads. One removable sample and real-engine lifecycle evidence are required before completion.
