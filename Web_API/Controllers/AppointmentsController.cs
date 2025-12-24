using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_API.Models;
using System.Security.Claims;
using Web_API.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Web_API.Controllers

{
    //[Route("api/[controller]")]
    //[Authorize]
    [Route("api/Appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly ProjectDbContext _context;

        public AppointmentsController(ProjectDbContext context)
        {
            _context = context;
        }

        //https://localhost:7085/api/Appointments/GetAppointments
        [HttpGet("GetAppointments",Name = "GetAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
        {
            try
            {
                // Include EVERYTHING so the appointment makes sense to the user
                var appointments = await _context.Appointments
                                                 .Include(a => a.Member)
                                                     .ThenInclude(m => m.person) 
                                                     
                                                     
                                                 .Include(a => a.Trainer)
                                                     .ThenInclude(t => t.person)                                              
                                             
                                                 .Include(a => a.Service)   
                                                 //.Select(a => new
                                                 //{
                                                 //    id = a.Trainer.Skills
                                                 //})
                                                 .ToListAsync();

                if (appointments == null || appointments.Count == 0)
                {
                    return NotFound("No appointments found.");
                }

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving appointments: " + ex.Message);
            }
        }

        //https://localhost:7085/api/Appointments/GetAppointment?id=
        [HttpGet("GetAppointment", Name = "GetAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Appointment>> GetAppointment(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid ID");
                }

                var appointment = await _context.Appointments
                                                .Include(a => a.Member).ThenInclude(m => m.person)
                                                .Include(a => a.Trainer).ThenInclude(t => t.person)
                                                .Include(a => a.Service)
                                                .FirstOrDefaultAsync(a => a.AppointmentID == id);

                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found.");
                }

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving appointment: " + ex.Message);
            }
        }

        //https://localhost:7085/api/Appointments/PostAppointment
        [HttpPost("PostAppointment",Name = "BookAppointment")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Used for "Already Booked" errors
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Appointment>> PostAppointment(Appointment appointment)
        {
            // 1. Basic Validation
            if (appointment == null)
            {
                return BadRequest("Appointment data is null.");
            }

            /*The Json To Work With:
             {
              "appointmentID": 8,
              "memberID": 1,
              "member": null,
              "trainerID": 1,
              "trainer": null,
              "serviceID": 1,
              "service": null,
              "startTime": "2025-12-11T09:00:00",
              "endTime": "2025-12-11T10:00:00",
              "fee": 150,
              "isApproved": true
            }
             */

            // 2. Time Logic Check
            if (appointment.StartTime >= appointment.EndTime)
            {
                return BadRequest("Start Time must be earlier than End Time.");
            }

            // 3. Existence Checks (Do these people exist?)
            if (!await _context.Members.AnyAsync(m => m.MemberID == appointment.MemberID))
                return BadRequest($"MemberID {appointment.MemberID} not found.");

            if (!await _context.Trainers.AnyAsync(t => t.TrainerID == appointment.TrainerID))
                return BadRequest($"TrainerID {appointment.TrainerID} not found.");

            if (!await _context.Services.AnyAsync(s => s.ServiceID == appointment.ServiceID))
                return BadRequest($"ServiceID {appointment.ServiceID} not found.");

            bool hasSkill = await _context.TrainerSkills.AnyAsync(ts =>
             ts.TrainerId == appointment.TrainerID &&
             ts.ServiceId == appointment.ServiceID);

            if (!hasSkill)
            {
                return BadRequest($"Trainer {appointment.TrainerID} is not qualified to provide Service {appointment.ServiceID}.");
            }

            // 4. CRITICAL: Conflict Check (Is the trainer already busy?)
            // Requirement: "Randevu saati... uygun değilse sistem kullanıcıyı uyarmalıdır."
            bool isTrainerBusy = await _context.Appointments.AnyAsync(a =>
                a.TrainerID == appointment.TrainerID &&
                (
                   appointment.StartTime < a.EndTime && appointment.EndTime > a.StartTime
                // Ends during another booking
                )
            );

            if (isTrainerBusy)
            {
                return Conflict("The trainer is already booked for this time slot."); // Returns 409
            }

            try
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetAppointment", new { id = appointment.AppointmentID }, appointment);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error booking appointment: " + ex.Message);
            }
        }

        //https://localhost:7085/api/Appointments/PutAppointment?id=
        [HttpPut("PutAppointment", Name = "UpdateAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutAppointment(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentID || appointment == null)
            {
                return BadRequest("ID mismatch or invalid data.");
            }

            /*The Json To Work With:
             {
              "appointmentID": 8,
              "memberID": 1,
              "member": null,
              "trainerID": 1,
              "trainer": null,
              "serviceID": 1,
              "service": null,
              "startTime": "2025-12-11T09:00:00",
              "endTime": "2025-12-11T10:00:00",
              "fee": 150,
              "isApproved": true
            }
             */


            if (appointment.StartTime >= appointment.EndTime)
            {
                return BadRequest("Start Time must be earlier than End Time.");
            }

            _context.Entry(appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(id))
                {
                    return NotFound($"Appointment with ID {id} not found.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Concurrency error during update.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating appointment: " + ex.Message);
            }

            return NoContent();
        }

        //https://localhost:7085/api/Appointments/DeleteAppointment?id=
        [HttpDelete("DeleteAppointment", Name = "DeleteAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ID");
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found.");
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting appointment: " + ex.Message);
            }
        }

        [HttpPost("Book")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Book(BookAppointmentDto dto)
        {
            // 1. Basic Validation
            if (dto.StartAt == DateTime.MinValue)
                return BadRequest("Error: The Date/Time was not received correctly.");

            if (dto.StartAt >= dto.EndAt)
                return BadRequest($"Error: Start time must be before End time.");

            // 2. Identify the User (Member)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = await _context.Members
                                       .Include(m => m.person)
                                       .FirstOrDefaultAsync(m => m.person.UserId == userId);

            if (member == null)
                return Unauthorized("Error: You must be a registered Member to book.");

            // 3. Validate Shift (Ignoring Year)
            int dayOfWeek = (int)dto.StartAt.DayOfWeek;

            var shifts = await _context.TrainerAvailabilities
                .Where(a => a.TrainerId == dto.TrainerId
                         && a.ServiceTypeId == dto.ServiceId
                         && a.DayOfWeek == dayOfWeek)
                .ToListAsync();

            if (!shifts.Any())
                return BadRequest($"Error: Trainer is not working on this day.");

            bool isShiftValid = shifts.Any(s =>
                dto.StartAt.TimeOfDay >= s.StartTime.TimeOfDay &&
                dto.EndAt.TimeOfDay <= s.EndTime.TimeOfDay
            );

            if (!isShiftValid)
                return BadRequest($"Error: The requested time is outside the trainer's shift.");

            // 4. Check for Double Booking (Overlapping appointments)
            bool isConflict = await _context.Appointments.AnyAsync(a =>
                a.TrainerID == dto.TrainerId &&
                a.StartTime < dto.EndAt &&
                a.EndTime > dto.StartAt
            );

            if (isConflict)
                return Conflict("Error: This time slot is already booked.");

            // 5. Save the Appointment
            var appointment = new Appointment
            {
                MemberID = member.MemberID,
                TrainerID = dto.TrainerId,

                // FIX 1: Use 'ServiceID' (Capital ID) to match your Model
                ServiceID = dto.ServiceId,

                // FIX 2: Do NOT set 'Date'. Your model only uses StartTime/EndTime.
                StartTime = dto.StartAt,
                EndTime = dto.EndAt,

                // Optional: You can set a default Fee or fetch it from Services
                Fee = 0,
                IsApproved = false // Automatically approve for now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Booking Successful!", AppointmentId = appointment.AppointmentID });
        }
        

        [Authorize(Roles = "Member")]
        [HttpGet("MyAppointments")]
        public async Task<IActionResult> MyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found.");

            var member = await _context.Members.FirstOrDefaultAsync(m => m.PersonID == person.PersonID);
            if (member == null) return BadRequest("You are not registered as a Member.");

            var list = await _context.Appointments
                .Where(a => a.MemberID == member.MemberID)
                .Include(a => a.Trainer).ThenInclude(t => t.person)
                .Include(a => a.Service)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new
                {
                    a.AppointmentID,
                    a.StartTime,
                    a.EndTime,
                    TrainerName = a.Trainer.person.Firstname + " " + a.Trainer.person.Lastname,
                    ServiceName = a.Service.ServiceName
                })
                .ToListAsync();

            return Ok(list);
        }


        ////https://localhost:7085/api/Appointments/Approve?id=
        //[Authorize(Roles = "Admin, Trainer")]
        //[HttpPut("Approve", Name = "Approve")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> Approve(int id)
        //{
        //    if (id < 0)
        //        return BadRequest("Id is wrong in the approve method");

        //    var appt = await _context.Appointments.FindAsync(id);
        //    if (appt == null)
        //        return NotFound("This appointment could not be found");

        //    if(!User.IsInRole("Admin") && User.IsInRole("Trainer"))
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (string.IsNullOrWhiteSpace(userId))
        //            return Unauthorized();

        //        var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
        //        if (person == null) return BadRequest("No Person profile found.");

        //        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
        //        if (trainer == null) return Forbid();

        //        if (appt.TrainerID != trainer.TrainerID)
        //            return Forbid("You can only approve your own appointments.");
        //    }
        //    appt.IsApproved = true;
        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Approved", appt.AppointmentID });
        //}

        // PUT: https://localhost:7085/api/Appointments/Approve?id=123
        [Authorize(Roles = "Trainer")]
        [HttpPut("Approve")]
        public async Task<IActionResult> Approve([FromQuery] int id)
        {
            if (id <= 0) return BadRequest("Invalid appointment id.");

            // 1) Get logged-in Identity UserId from the cookie (shared auth)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // 2) Map Identity user -> Person -> Trainer
            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found for this user.");

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            // 3) Load appointment + verify it belongs to THIS trainer
            var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentID == id);
            if (appt == null) return NotFound($"Appointment {id} not found.");

            if (appt.TrainerID != trainer.TrainerID)
                return Forbid(); // 403 (trying to approve someone else's appointment)

            // 4) Approve
            if (appt.IsApproved)
                return Ok(new { message = "Already approved." });

            appt.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Approved successfully." });
        }

        // https://localhost:7085/api/Appointments/ 
        [Authorize(Roles = "Trainer")]
        [HttpGet("MyTrainerAppointments")]
        public async Task<IActionResult> MyTrainerAppointments([FromQuery] bool pendingOnly = false, [FromQuery] bool upcomingOnly = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found for this user.");

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.PersonID == person.PersonID);
            if (trainer == null) return BadRequest("You are not registered as a Trainer.");

            var q = _context.Appointments
                .Where(a => a.TrainerID == trainer.TrainerID)
                .Include(a => a.Member).ThenInclude(m => m.person)
                .Include(a => a.Service)
                .AsQueryable();

            if (pendingOnly)
                q = q.Where(a => a.IsApproved == false);

            if (upcomingOnly)
                q = q.Where(a => a.StartTime >= DateTime.Now);

            var list = await q
                .OrderBy(a => a.StartTime)
                .Select(a => new
                {
                    appointmentID = a.AppointmentID,
                    startTime = a.StartTime,
                    endTime = a.EndTime,
                    isApproved = a.IsApproved,
                    fee = a.Fee,
                    memberName = a.Member.person.Firstname + " " + a.Member.person.Lastname,
                    serviceName = a.Service.ServiceName
                })
                .ToListAsync();

            return Ok(list);
        }

        //https://localhost:7085/api/Appointments/CancelAppointment?id=
        [HttpDelete("CancelAppointment", Name = "CancelAppointment")]
        [Authorize]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the appointment including the Member/Person data to verify ownership
            var appointment = await _context.Appointments
                .Include(a => a.Member).ThenInclude(m => m.person)
                .Include(a => a.Trainer).ThenInclude(t => t.person)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
                return NotFound("Appointment not found.");

            // Security Check: Ensure the logged-in user owns this appointment
            // User can be the Member OR the Trainer involved.
            bool isMember = appointment.Member?.person?.UserId == userId;
            bool isTrainer = appointment.Trainer?.person?.UserId == userId;

            if (!isMember && !isTrainer)
            {
                return Unauthorized("You are not authorized to cancel this appointment.");
            }

            // Optional: Prevent canceling past appointments
            if (appointment.StartTime < DateTime.Now)
            {
                return BadRequest("Cannot cancel a completed appointment.");
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 Success
        }


        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentID == id);
        }
    }
}