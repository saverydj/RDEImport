namespace ToolsForReuse
{
    public class UnitMod
    {
        public double Mult { get; private set; }
        public double Add { get; private set; }

        public UnitMod(double mult, double add)
        {
            SetUnits(mult, add);
        }

        public UnitMod(string units)
        {
            SetUnits(1.0, 0.0);

            if (units == "km") SetUnits(1000.0, 0.0);
            if (units == "m") SetUnits(1.0, 0.0);
            if (units == "dm") SetUnits(1.0 / 10.0, 0.0);
            if (units == "cm") SetUnits(1.0 / 100.0, 0.0);
            if (units == "mm") SetUnits(1.0 / 1000.0, 0.0);
            if (units == "um") SetUnits(1.0 / 1000000.0, 0.0);

            if (units == "mi") SetUnits(1609.344, 0.0);
            if (units == "yd") SetUnits(0.9144, 0.0);
            if (units == "ft") SetUnits(0.3048, 0.0);
            if (units == "in") SetUnits(0.0254, 0.0);

            if (units == "kph" || units == "km/h") SetUnits(1000.0 / 3600.0, 0.0);
            if (units == "mph" || units == "mi/h") SetUnits(0.44704, 0.0);
            if (units == "mps" || units == "m/s") SetUnits(1.0, 0.0);

            if (units == "°C" || units == "degC") SetUnits(1.0, 273.15);
            if (units == "°F" || units == "degF") SetUnits(5.0 / 9.0, 2298.35 / 9.0);
            if (units == "K" || units == "degK") SetUnits(1.0, 0.0);

            if (units == "%") SetUnits(1.0 / 100.0, 0.0);
        }

        private void SetUnits(double mult, double add)
        {
            Mult = mult;
            Add = add;
        }

        public double Convert(double input)
        {
            return input * Mult + Add;
        }
    }
}
