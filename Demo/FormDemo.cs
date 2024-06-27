﻿using Gradio.Net;

namespace demo;

public static class FormDemo
{
    public static async Task Create()
    {
        gr.Markdown("# FormDemo Demo");

        gr.HTML(value: "<p style='margin-top: 1rem, margin-bottom: 1rem'>This <em>example</em> was <strong>written</strong> in <a href='https://en.wikipedia.org/wiki/HTML' _target='blank'>HTML</a> </p>");

        gr.Markdown("## Get Component Property");
        using (gr.Column())
        {
            Textbox text1 = gr.Textbox(type: Gradio.Net.Enums.TextboxType.Password);
            Dropdown dropdown1 = gr.Dropdown(choices: ["First Choice", "Second Choice", "Third Choice"]);
            Checkbox checkbox1 = gr.Checkbox();
            CheckboxGroup checkboxGroup1 = gr.CheckboxGroup(choices: ["First Choice", "Second Choice", "Third Choice"]);
            MultimodalTextbox multimodalTextbox1 = gr.MultimodalTextbox(interactive: true);
            Number number1 = gr.Number();
            Radio radio1 = gr.Radio(choices: ["First Choice", "Second Choice", "Third Choice"]);
            Slider slider1 = gr.Slider();

            Textbox text_Result = gr.Textbox(label: "Form Value", interactive: false);

            Button btn = gr.Button("Run");
            await btn.Click(fn: async (input) => gr.Output($@"
                Textbox: {Textbox.Payload(input.Data[0])}
                Dropdown: {string.Join(", ", Dropdown.Payload(input.Data[1]))}
                Checkbox: {Checkbox.Payload(input.Data[2])}
                CheckboxGroup: {string.Join(", ", CheckboxGroup.Payload(input.Data[3]))}
                MultimodalTextbox: {MultimodalTextbox.Payload(input.Data[4]).Files.FirstOrDefault()?.OrigName}
                Number: {Number.Payload(input.Data[5])}
                Radio: {string.Join(", ", Radio.Payload(input.Data[6]))}
                Slider: {Slider.Payload(input.Data[7])}
                "), inputs: [text1, dropdown1, checkbox1, checkboxGroup1, multimodalTextbox1, number1, radio1, slider1], outputs: [text_Result]);
        }

        gr.Markdown("## Change Component Property");

        using (gr.Row())
        {
            var categories = new[] { "Long-Short Equity", "Long Government", "Multisector Bond", "Emerging Markets Bond", "Corporate Bond", "Intermediate Government", "Inflation-Protected Bond", "Intermediate-Term Bond", "Muni National Long", "Unknown", "High Yield Muni", "Long-Term Bond", "Muni California Long", "Muni National Interm", "Nontraditional Bond", "World Bond", "Short Government", "Muni National Short", "Short-Term Bond", "Preferred Stock", "Ultrashort Bond", "High Yield Bond", "Muni New York Long", "Emerging-Markets Local-Currency Bond", "Miscellaneous Region", "Bank Loan", "Commodities Broad Basket", "Japan Stock", "World Allocation", "Tactical Allocation", "Large Value", "Foreign Large Growth", "Energy Limited Partnership", "Foreign Small/Mid Blend", "Foreign Large Value", "Foreign Large Blend", "Europe Stock", "Allocation--50% to 70% Equity", "Financial", "Diversified Emerging Mkts", "Industrials", "Mid-Cap Blend", "Large Growth", "Communications", "Diversified Pacific/Asia", "Foreign Small/Mid Value", "Convertibles", "Small Value", "Latin America Stock", "Equity Energy", "Natural Resources", "Real Estate", "Large Blend", "Small Blend", "Consumer Cyclical" };
            var category_dropdown = gr.Dropdown(choices: categories, label: "Category");
            var symbol_dropdown = gr.Dropdown(label: "Symbols", choices: []);

            // Update the symbols dropdown when the category changes
            category_dropdown.Change(update_symbols, inputs: [category_dropdown], outputs: [category_dropdown, symbol_dropdown]);
        }

        using (gr.Row())
        {
            var click_count = gr.State(value: 0);

            var btn = gr.Button("Show");
            var textCount = gr.Textbox(label: "Count", visible: false);
            btn.Click(update_props, inputs: [click_count], outputs: [click_count, btn, textCount]);
        }
    }

    private static async Task<Output> update_props(Input input)
    {
        var click_count = int.Parse(input.Data[0].ToString()) + 1;
        return gr.Output(click_count, gr.Button(value: (click_count + 1) % 2 == 0 ? "Hide" : "Show"), gr.Textbox(value: ((click_count + 1) / 2).ToString(), visible: (click_count + 1) % 2 == 0));

    }

    private static async Task<Output> update_symbols(Input input)
    {
        var symbols = new[] { "FFIU", "IGEB", "VCIT", "FCOR", "SKOR", "KORP", "LQDI" };
        if (((string[])input.Data[0])[0].ToString() == "Long-Short Equity")
            gr.Warning("This is some warning.");

        ////await Task.Delay(1000);

        ////gr.Info("This is some info.");


        return gr.Output(gr.Dropdown(value: "", interactive: true), gr.Dropdown(choices: symbols, interactive: true));
    }
}
