import React, {useState} from "react";
import { FormGroup } from "./FormGroup";
import Countries from "../../../data/countries";

export function FlagGroup(props) {
    const countries = Object.keys(Countries);
    const [input, setInput] = useState("");
    const [error, setError] = useState(true);
    const inputChanged = ev => {
        let value = ev.target.value;
        let success = false;
        if(Countries[value])
            success = true;
        setInput(value)
        if(error === success)
            setError(!success);
    }
    const addClicked = () => {
        if(Countries[input]){
            props.setCountry(Countries[input].code)
            setInput("");
            setError(true);
        }
    }
    return <div className="pb-3">
        <FormGroup lock={props.lock} flag={props.country} type="search" error={error} holder={props.holder} value={input}
            list={countries} addTitle={props.add} inputChanged={inputChanged} add={addClicked}/>
    </div>
}