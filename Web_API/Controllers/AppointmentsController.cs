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
                    (appointment.StartTime >= a.StartTime && appointment.StartTime < a.EndTime) || // Starts during another booking
                    (appointment.EndTime > a.StartTime && appointment.EndTime <= a.EndTime)     // Ends during another booking
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
        public async Task<IActionResult> Book(BookAppointmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // prevent cross-day bookings (because availability is day-based)
            if (dto.StartAt.Date != dto.EndAt.Date)
                return BadRequest("StartAt and EndAt must be on the same date.");

            if (dto.StartAt >= dto.EndAt)
                return BadRequest("StartAt must be < EndAt.");

            // Current user => Member
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
            if (person == null) return BadRequest("No Person profile found.");

            var member = await _context.Members.FirstOrDefaultAsync(m => m.PersonID == person.PersonID);
            if (member == null) return BadRequest("You are not registered as a Member.");

            // Trainer must exist
            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.TrainerID == dto.TrainerId);
            if (trainer == null) return BadRequest("Trainer does not exist.");

            // Service must exist
            bool serviceExists = await _context.Services.AnyAsync(s => s.ServiceID == dto.ServiceId);
            if (!serviceExists) return BadRequest("Service does not exist.");

            // Trainer must have that skill
            bool trainerHasSkill = await _context.TrainerSkills.AnyAsync(ts =>
                ts.TrainerId == dto.TrainerId && ts.ServiceId == dto.ServiceId);

            if (!trainerHasSkill)
                return BadRequest("Trainer does not offer this service.");

            // Must be inside availability
            int day = (int)dto.StartAt.DayOfWeek;

            // Availability stores time in DateTime (fixed date). We compare using same fixed date.
            var baseDate = new DateTime(2000, 1, 1);
            var reqStart = baseDate.Add(dto.StartAt.TimeOfDay);
            var reqEnd = baseDate.Add(dto.EndAt.TimeOfDay);

            bool insideAvailability = await _context.TrainerAvailabilities.AnyAsync(a =>
                a.TrainerId == dto.TrainerId &&
                a.DayOfWeek == day &&
                a.ServiceTypeId == dto.ServiceId &&
                reqStart >= a.StartTime &&
                reqEnd <= a.EndTime);

            if (!insideAvailability)
                return BadRequest("Requested time is outside trainer availability.");

            // Prevent overlap (trainer)
            bool trainerOverlap = await _context.Appointments.AnyAsync(a =>
                a.TrainerID == dto.TrainerId &&
                dto.StartAt < a.EndTime && dto.EndAt > a.StartTime);

            if (trainerOverlap)
                return Conflict("Trainer already has an appointment at this time.");

            // Prevent overlap (member)
            bool memberOverlap = await _context.Appointments.AnyAsync(a =>
                a.MemberID == member.MemberID &&
                dto.StartAt < a.EndTime && dto.EndAt > a.StartTime);

            if (memberOverlap)
                return Conflict("You already have an appointment at this time.");

            // Create appointment
            var appt = new Appointment
            {
                MemberID = member.MemberID,
                TrainerID = dto.TrainerId,
                ServiceID = dto.ServiceId,
                StartTime = dto.StartAt,
                EndTime = dto.EndAt
            };

            _context.Appointments.Add(appt);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Booked", appt.AppointmentID });
        }

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

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentID == id);
        }
    }
}