import React, { useState, useContext } from "react";
import { Context } from "../../../data/Context";
import { FormGroup } from "./FormGroup";
import Langs from "../../../data/langs_full";
import { faTimes } from "@fortawesome/free-solid-svg-icons"
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"

export function FilterGroup(props) {
    const context = useContext(Context);
    const [input, setInput] = useState("");
    const [error, setError] = useState(true);
    const [filters, setFilters] = useState(context.filters);
    const inputChanged = ev => {
        let value = ev.target.value
        setInput(ev.target.value);
        let valid = false;
        for (let key in Langs) {
            if (key === value) {
                valid = true;
                break;
            }
        }
        if (error === valid)
            setError(!valid);
    }
    const addFilter = () => {
        let key = Langs[input];
        if (key) {
            setInput("");
            setError(true);
            if (!filters[key]) {
                let temp = {}
                temp = { ...filters, [key]: input };
                context.changeFilters(temp);
                setFilters(temp);
            }
        }

    }
    const deleteFilter = key => {
        let temp = {}
        for (let kkey in filters)
            if (kkey !== key) temp[kkey] = filters[kkey];
        context.changeFilters(temp);
        setFilters(temp);
    }
    return <>
        <FormGroup label={props.label} value={input} type="search" error={error} holder={props.holder} lang={true}
            inputChanged={inputChanged} list={Object.keys(Langs)} add={addFilter} addTitle={props.add} />
        {
            filters && Object.keys(filters).length > 0 &&
            <h5 className="border p-3">
                {
                    Object.entries(filters).map(([key, value]) =>
                        <span key={key} className="badge badge-info m-1">{value}
                            <span className="badge badge-dark badge-times"
                                onClick={() => deleteFilter(key)}>
                                <FontAwesomeIcon icon={faTimes} /></span></span>)
                }
            </h5>
        }
    </>
}