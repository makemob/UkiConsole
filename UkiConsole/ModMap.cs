using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UkiConsole
{
    class ModMap
    {
        // This is entirely the wrong place for this, I think
        public static List<int> PositionRegisters = new List<int>(){ 218, 299};
        public static List<int> ControlRegisters = new List<int>() { 208,209 };
        public static List<int> ControlAddresses = new List<int>() { 0,240 };

        public static  Dictionary<int, String> EstopLabel = new()
        {
            { 0, "No Estop" },
            {1, "Remote" },
            {2, "Current (I)"},
            { 3, "Current (O)"},
            { 4, "Overvolt"},
            { 5, "Extension (I)"},
            { 6, "Extension (O)"},
            { 7, "Encoder"},
            { 8, "Heartbeat"}
        };
        public  enum RegMap
        {
            MB_SCARAB_ID1 = 0,
            // ...
            MB_SCARAB_ID15 = 15,
            MB_BOARD_VERSION,
            MB_FW_VERSION_MAJOR,
            MB_FW_VERSION_MINOR,
            MB_MODBUS_ERROR_COUNT,
            MB_UPTIME_MSW,
            MB_UPTIME_LSW,

            MB_BRIDGE_CURRENT = 100,
            MB_BATT_VOLTAGE,
            MB_MAX_BATT_VOLTAGE,
            MB_MIN_BATT_VOLTAGE,
            MB_BOARD_TEMPERATURE,
            MB_EXT_1_ADC,
            MB_EXT_2_ADC,
            MB_EXT_1_DIG,
            MB_EXT_2_DIG,
            MB_EXT_3_DIG,
            MB_EXT_4_DIG,
            MB_EXT_5_DIG,
            MB_EXT_6_DIG,
            MB_BLUE_LED,
            MB_GREEN_LED,
            MB_INWARD_ENDSTOP_STATE_DEPRECATED,
            MB_OUTWARD_ENDSTOP_STATE_DEPRECATED,
            MB_POSITION_ENCODER_COUNTS,

            MB_MOTOR_SETPOINT = 200,
            MB_MOTOR_SPEED,
            MB_MOTOR_ACCEL,
            MB_CURRENT_LIMIT_INWARD,
            MB_CURRENT_LIMIT_OUTWARD,
            MB_EXTENSION_LIMIT_INWARD,  // formerly MB_CURRENT_TRIPS_INWARD_DEPRECATED,
            MB_EXTENSION_LIMIT_OUTWARD, // formerly MB_CURRENT_TRIPS_OUTWARD_DEPRECATED
            MB_POSITION_ENCODER_SCALING, // formerly MB_VOLTAGE_TRIPS_DEPRECATED,
            MB_ESTOP,
            MB_RESET_ESTOP,    // Write 0x5050 to reset emergency stop
            MB_MOTOR_PWM_FREQ_MSW,
            MB_MOTOR_PWM_FREQ_LSW,
            MB_MOTOR_PWM_DUTY_MSW,
            MB_MOTOR_PWM_DUTY_LSW,
            MB_INWARD_ENDSTOP_COUNT_DEPRECATED,
            MB_OUTWARD_ENDSTOP_COUNT_DEPRECATED,
            MB_HEARTBEAT_EXPIRIES_DEPRECATED,

            MB_GOTO_POSITION = 218,
            MB_GOTO_SPEED_SETPOINT,
            MB_FORCE_CALIBRATE_ENCODER, // write 0xA0A0 to force encoder to calibrate to zero in current position

            MB_EXTENSION = 299,
            MB_ESTOP_STATE = 300,
            MB_CURRENT_TRIPS_INWARD,
            MB_CURRENT_TRIPS_OUTWARD,
            MB_INWARD_ENDSTOP_STATE,
            MB_OUTWARD_ENDSTOP_STATE,
            MB_INWARD_ENDSTOP_COUNT,
            MB_OUTWARD_ENDSTOP_COUNT,
            MB_VOLTAGE_TRIPS,
            MB_HEARTBEAT_EXPIRIES,
            MB_EXTENSION_TRIPS_INWARD,
            MB_EXTENSION_TRIPS_OUTWARD,
            MB_ENCODER_FAIL_TRIPS,

            // Position info etc. = 400

            MB_UNLOCK_CONFIG = 9000,    // Write 0xA0A0 to unlock regs, anything else to lock
            MB_MODBUS_ADDRESS,
            MB_OPERATING_MODE,   // eg. Limit switches, encoders
            MB_OPERATING_CONFIG, // specific config for the selected mode
            MB_DEFAULT_CURRENT_LIMIT_INWARD,
            MB_DEFAULT_CURRENT_LIMIT_OUTWARD,
            MB_MAX_CURRENT_LIMIT_INWARD,
            MB_MAX_CURRENT_LIMIT_OUTWARD,
            MB_HEARTBEAT_TIMEOUT,  // seconds until heartbeat timer trips
            MB_ENCODER_FAIL_TIMEOUT,  // Max milliseconds between encoder pulses before timeout


            NUM_MODBUS_REGS

        }
        



        public static Dictionary<String, RegMap> confMap = new Dictionary<string, RegMap>()
        {
            {"inwardCurrentLimit", RegMap.MB_CURRENT_LIMIT_INWARD },
            {"outwardCurrentLimit", RegMap.MB_CURRENT_LIMIT_OUTWARD },
            {"outwardExtensionLimit", RegMap.MB_EXTENSION_LIMIT_OUTWARD  },
            {"inwardExtensionLimit", RegMap.MB_EXTENSION_LIMIT_INWARD },
             {"acceleration", RegMap.MB_MOTOR_ACCEL },
        };

        public static Dictionary<int, List<RegMap>> LimitMap = new Dictionary<int, List<RegMap>>()
        {
            {(int)RegMap.MB_GOTO_POSITION, new List<RegMap>(){RegMap.MB_EXTENSION_LIMIT_INWARD,RegMap.MB_EXTENSION_LIMIT_OUTWARD } },
            {(int)RegMap.MB_MOTOR_ACCEL, new List<RegMap>(){RegMap.MB_MOTOR_ACCEL} },
           // {(int)RegMap.MB_GOTO_SPEED_SETPOINT,  }
        };




        public static Dictionary<String, RegMap> RegName = new Dictionary<String, RegMap> {
            { "Estop", RegMap.MB_ESTOP_STATE },
            {"Speed", RegMap.MB_MOTOR_SPEED },
          //  {"Target Speed", RegMap.MB_MOTOR_SETPOINT},
            {"Accel", RegMap.MB_MOTOR_ACCEL },
         //   {"Target Position", RegMap.MB_GOTO_POSITION },
            {"Position", RegMap.MB_EXTENSION },
             {"EncPosition", RegMap.MB_POSITION_ENCODER_COUNTS },
            {"Microswitch", RegMap.MB_INWARD_ENDSTOP_STATE },

        };


        public static Dictionary<String, RegMap> TargetName = new Dictionary<String, RegMap> {
            //When they are targets, "Speed" depends on the move type, so see below.
            //Target Accel is always MB_MOTOR_ACCEL, Target Position is
            // always MB_GOTO_POSITION
            {"Accel", RegMap.MB_MOTOR_ACCEL },
            {"Position", RegMap.MB_GOTO_POSITION }

        };
        public static RegMap regFromTarget(String movetype, String target)
        {
            RegMap targetreg = 0; 
            Dictionary<String, RegMap> targetmap;
            MoveTypeTargets.TryGetValue(movetype, out targetmap);
            if (targetmap is not null)
            {
                targetmap.TryGetValue(target, out targetreg);
            }
            // No special target value, try the commonly written ones
            //Since this is a move target, if it's not mapped or in the targets, return 0
            if (targetreg == 0)
            {
                TargetName.TryGetValue(target, out targetreg);
            }

            return targetreg;
        }

        public static Dictionary<string, Dictionary<String, RegMap>> MoveTypeTargets = new()
        {
            {
                "Position", new() { { "Speed", RegMap.MB_GOTO_SPEED_SETPOINT } } 
            },


            { 
                "Speed", new() { { "Speed", RegMap.MB_MOTOR_SETPOINT } }
            }

        
        };


        // in CSV land, we get "Speed" and "position". 
        // In UDP land, we get reg numbers.
        // So we reverse map to "speed" and "position"
        // This all needs refactoring....
        public static Dictionary<string, string> TargetRevMap = new()
        {


            { "218", "Position" },
           // { "219", "Speed" },


        };
        public static String RevMap(int regnum)
        {
            foreach (KeyValuePair<String, RegMap> kvp in RegName)
            {
                if ((int)kvp.Value == regnum){
                    //In theory it's one to one....
                    return kvp.Key;
                }
               
            }
            return regnum.ToString();
        }

        
        public ModMap() { }

        
        
    }
}
